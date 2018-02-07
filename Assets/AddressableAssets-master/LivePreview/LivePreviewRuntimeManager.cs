#if LIVE_PREVIEW
using ResourceManagement;
using ResourceManagement.AsyncOperations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace AddressableAssets.LivePreview
{
    public partial class LivePreviewRuntimeManager : MonoBehaviour
    {
        [Serializable]
        public class RDMessage
        {
            public static int gCurMessageId;
            public string sessionId;
            public int messageId;
            public object messageObject;
            public RDMessage(string id, object msgObject)
            {
                sessionId = id;
                messageObject = msgObject;
                messageId = gCurMessageId++;
            }

            public RDMessage() { }
        }
        [Serializable]
        public class RDResponse
        {
            public int messageId;
            public object messageObject;
            public RDResponse(int msgId, object msgObject)
            {
                messageObject = msgObject;
                messageId = msgId;
            }

            public RDResponse() { }
        }

        internal class MessageResponseHandle
        {
            public int messageId;
            public object result;

            public MessageResponseHandle(int msgId)
            {
                messageId = msgId;
            }

            public object Block(int timeout = 5000)
            {
                while (result == null && PlayerConnection.instance.BlockUntilRecvMsg(kRemoteAssetDatabaseGuid, timeout)) ;
                return result;
            }
        }

        internal string SessionId;

        private Dictionary<int, MessageResponseHandle> mActiveRequests = new Dictionary<int, MessageResponseHandle>();

        public static Guid kRemoteAssetDatabaseGuid = new Guid(3, 2, 1, new byte[] { 0, 1, 32, 3, 4, 9, 6, 7 });
        internal MessageResponseHandle SendMessage(object obj)
        {
            //string SessionID = Application.platform.ToString() + ((Application.platform == RuntimePlatform.WindowsPlayer) ? "64" : "");
            RDMessage outMsg = new RDMessage(SessionId, obj);
            byte[] msgBytes = MessageUtil.Serialize(outMsg);
            MessageResponseHandle response = new MessageResponseHandle(outMsg.messageId);
            mActiveRequests.Add(outMsg.messageId, response);
            PlayerConnection.instance.Send(kRemoteAssetDatabaseGuid, msgBytes);
            //byte[] resultBin = ClientSessionManager.SendMessageToHost("RemoteAssetDatabase", msgBytes);
            return response;
        }

        internal object SendMessageSync(object obj)
        {
            return SendMessage(obj).Block();
        }

        internal void OnPlayerConnectionMessage(UnityEngine.Networking.PlayerConnection.MessageEventArgs args)
        {
            RDResponse obj = (RDResponse)MessageUtil.Deserialize(args.data);
            mActiveRequests[obj.messageId].result = obj.messageObject;
        }

        void OnEnable()
        {
            PlayerConnection.instance.Register(kRemoteAssetDatabaseGuid, OnPlayerConnectionMessage);
            DontDestroyOnLoad(gameObject);
        }

        internal void Update()
        {
            UpdateTasks();
            UpdateHotReload();
        }

        private static string GetBundlePath(RuntimeBundleIdentfier id)
        {
            string bundleFilename = Path.Combine(Application.dataPath, string.Format("{0}_{1}", id.BundleName, id.Version));
            return bundleFilename;
        }

        private IAsyncOperation<T> LoadAssetCustomAsync<T>(string key, DLPResourceManagerMessageLocate.AddressType type) where T : class
        {
            Debug.LogFormat("LivePreviewRuntimeManager.LoadAssetCustomAsync({0})", key);
            
            var task = new LivePreviewTaskLoad(this, key, typeof(T));
            mTaskQueue.Add(task);

            return new DLPAsyncLoadOperation<T>(task, key);
        }

        public IAsyncOperation<T> LoadAssetByGUIDAsync<T>(string guid) where T : class
        {
            return LoadAssetCustomAsync<T>(guid, DLPResourceManagerMessageLocate.AddressType.GUID);
        }

        private int m_LastCheckIndex;
        private float mReloadCheckTime = 2.0f;
        private float mTimeSinceCheck = 0;
        private LivePreviewTaskPatch m_PatchTask;
        private void UpdateHotReload()
        {
            if (m_PatchTask == null)
            {
                mTimeSinceCheck += Time.unscaledDeltaTime;
                if (mTimeSinceCheck > mReloadCheckTime)
                {
                    m_PatchTask = new LivePreviewTaskPatch(this, m_LastCheckIndex);
                    mTaskQueue.Add(m_PatchTask);
                }
            }
            else if (m_PatchTask != null && !mTaskQueue.Contains(m_PatchTask))
            {
                m_LastCheckIndex = m_PatchTask.GetNewCheckIndex();
                m_PatchTask = null;
                mTimeSinceCheck = 0;
            }
        }

        public bool Release(string id)
        {
            return false;
        }

        private class DLPAsyncLoadOperation<T> : AsyncOperationBase<T> where T : class
        {
            internal DLPAsyncLoadOperation(LivePreviewTaskLoad task, string id)
            {
                task.OnComplete += InternalEvent_OnComplete;
            }

            private void InternalEvent_OnComplete(object result)
            {
                m_result = (T)result;
                InvokeCompletionEvent();
            }
        }

        internal class DLPAsyncLoadOperationStub<T> : AsyncOperationBase<T>
        {
            public DLPAsyncLoadOperationStub(T obj, string id)
            {
                SetResult(obj);
            }
        }

        internal class BundleLoadInfo
        {
            internal BundleLoadInfo(RuntimeBundleIdentfier id)
            {
                Identifier = id;
                State = LoadState.None;
            }
            internal enum LoadState
            {
                None,
                Loading,
                Patching,
                Ready,
            }

            public AssetBundleCreateRequest Request;
            public AssetBundle Bundle;
            public RuntimeBundleIdentfier Identifier;
            public LoadState State;
        }

        private Dictionary<string, BundleLoadInfo> mBundleInfos = new Dictionary<string, BundleLoadInfo>();

        bool AreBundleOperationsActive(RuntimeBundleIdentfier[] ids)
        {
            foreach (var id in ids)
            {
                BundleLoadInfo info;
                if (mBundleInfos.TryGetValue(id.BundleName, out info))
                    if (info.State == BundleLoadInfo.LoadState.Loading || info.State == BundleLoadInfo.LoadState.Patching)
                        return true;
            }
            return false;
        }

        BundleLoadInfo LoadBundleAsync(RuntimeBundleIdentfier id)
        {
            BundleLoadInfo info = mBundleInfos[id.BundleName];
            Debug.Assert(info.State == BundleLoadInfo.LoadState.None);
            info.Identifier = id;
            info.State = BundleLoadInfo.LoadState.Loading;
            info.Request = AssetBundle.LoadFromFileAsync(GetBundlePath(id));
            info.Request.completed += item =>
                {
                    info.Bundle = info.Request.assetBundle;
                    info.State = BundleLoadInfo.LoadState.Ready;
                };
            return info;
        }

        List<LivePreviewTask> mTaskQueue = new List<LivePreviewTask>();
        private void UpdateTasks()
        {
            List<LivePreviewTask> toRemoveList = null;
            foreach (LivePreviewTask op in mTaskQueue)
            {
                LivePreviewTask.Status status = op.Update();
                if (status == LivePreviewTask.Status.Blocked)
                    break;
                if (status == LivePreviewTask.Status.Complete)
                {
                    toRemoveList = (toRemoveList == null) ? new List<LivePreviewTask>() : toRemoveList;
                    toRemoveList.Add(op);
                }
            }
            if (toRemoveList != null)
                mTaskQueue.RemoveAll(i => toRemoveList.Contains(i));
            
        }
    }
}
#endif