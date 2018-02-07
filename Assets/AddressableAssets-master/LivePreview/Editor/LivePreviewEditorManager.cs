#if false //needs to be updated to lates AB-BP API
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditorInternal.RestService;
using UnityEngine;

namespace AddressableAssets.LivePreview
{
    class RemoteAssetDatabaseAssetWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (LivePreviewEditorManager.instance != null)
            {
                LivePreviewEditorManager.instance.OnAssetsImported(importedAssets);
            }
        }
    }

    class RemoteAddressableAssetCache
    {
        private Dictionary<string, BundleIdentifier[]> mKeyToBundles = new Dictionary<string, BundleIdentifier[]>();
        private Dictionary<string, uint> mBundleNameToVersionHash = new Dictionary<string, uint>();

        public void NotifyChangedBundles(string[] bundleNames)
        {
            foreach (string name in bundleNames)
                if (mBundleNameToVersionHash.ContainsKey(name))
                    mBundleNameToVersionHash[name] = 0;
        }

        public BundleIdentifier[] GetCachedBundleList(string key)
        {
            BundleIdentifier[] result = null;
            if (mKeyToBundles.TryGetValue(key, out result))
            {
                foreach (BundleIdentifier id in result)
                {
                    if (mBundleNameToVersionHash[id.BundleName] != id.VersionHash)
                    {
                        mKeyToBundles.Remove(key);
                        return null;
                    }
                }
            }
            return result;
        }

        public void SetCachedBundleList(string key, BundleIdentifier[] bundles)
        {
            foreach (BundleIdentifier id in bundles)
            {
                mBundleNameToVersionHash[id.BundleName] = id.VersionHash;
                mKeyToBundles[key] = bundles;
            }
        }
    }


    public class LivePreviewEditorManager : ScriptableSingleton<LivePreviewEditorManager>
    {
        private static bool m_DebugPrints = true;
        internal class SessionContext
        {
            public LivePreviewBuilder builder;
            public RemoteAddressableAssetCache keyToBundlesCache = new RemoteAddressableAssetCache();
        }

        public static string[] DirectoryLocatorCallback(string sessionId)
        {
            return new string[] { Session.GetPlayerDataDirectory(sessionId) };
        }

        private static bool FileLocator(ref string outFilename)
        {
            string filename = outFilename;
            filename = filename.Substring(1);
            int i = filename.IndexOf('/');
            string sessionId = filename.Substring(0, i);

            // strip /Data/
            filename = filename.Substring("/Data/".Length + sessionId.Length);

            string dataDir = Session.GetDynamicDataDirectory(sessionId);
            filename = Path.Combine(dataDir, filename);
            if (File.Exists(filename))
            {
                outFilename = Path.GetFullPath(filename);
                return true;
            }
            return false;
        }

        [InitializeOnLoadMethod]
        private static void StaticInit()
        {
            // forces creation of the singleton
            
            if (LivePreviewEditorManager.instance == null)
                Debug.Log("EditorListener::StaticInit FAILED!");
            
            PlayerDataFileLocator.Register(new PlayerDataFileLocator.Locator(FileLocator));
        }

        internal void OnPlayerConnectionMessage(UnityEngine.Networking.PlayerConnection.MessageEventArgs args)
        {
            LivePreviewRuntimeManager.RDMessage msgObject = (LivePreviewRuntimeManager.RDMessage)MessageUtil.Deserialize(args.data);
            Session session = Session.CreateFromId(msgObject.sessionId); // TODO cache this locally
            object result = DataRequestObject(session, msgObject.messageObject);
            LivePreviewRuntimeManager.RDResponse response = new LivePreviewRuntimeManager.RDResponse(msgObject.messageId, result);
            byte[] responseData = MessageUtil.Serialize(response);
            EditorConnection.instance.Send(LivePreviewRuntimeManager.kRemoteAssetDatabaseGuid, responseData, args.playerId);
        }

        private void OnEnable()
        {
            EditorConnection.instance.Register(LivePreviewRuntimeManager.kRemoteAssetDatabaseGuid, OnPlayerConnectionMessage);
        }

        private void OnDisable()
        {
            EditorConnection.instance.Unregister(LivePreviewRuntimeManager.kRemoteAssetDatabaseGuid, OnPlayerConnectionMessage);
        }

        Dictionary<string, SessionContext> mContexts;

        public LivePreviewEditorManager()
        {
            mContexts = new Dictionary<string, SessionContext>();
        }

        public void OnAssetsImported(string[] assets)
        {
            if (mContexts.Count != 0)
            {
                GUID[] guids = new GUID[assets.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    string guidString = AssetDatabase.AssetPathToGUID(assets[i]);
                    guids[i] = new GUID(guidString);
                }
                foreach (SessionContext c in mContexts.Values)
                {
                    int idx = c.builder.GetChangedIndex();
                    int newIdx;
                    foreach (GUID guid in guids)
                        c.builder.NotifyAssetModified(guid);
                    string[] changedBundleNames = c.builder.GetStaleBundles(idx, out newIdx);
                    c.keyToBundlesCache.NotifyChangedBundles(changedBundleNames);
                }
            }
        }

        internal SessionContext InitContext(Session session)
        {
            if (!mContexts.ContainsKey(session.sessionID))
            {
                SessionContext context = new SessionContext();
                context.builder = new LivePreviewBuilder(session.buildTarget, session.buildTargetGroup, session.typeDB, Session.GetDynamicDataDirectory(session.sessionID));
                //context.builder.Setup(session.typeDB, session.GetDynamicDataDirectory());
                mContexts.Add(session.sessionID, context);
            }

            return mContexts[session.sessionID];
        }

        internal object DataRequestObject(Session session, object msgObject)
        {
            SessionContext c = InitContext(session);
            if (msgObject is DLPResourceManagerMessageLocate.Query)
            {
                var msg = (DLPResourceManagerMessageLocate.Query)msgObject;
                BundleIdentifier[] bundles;

                bundles = c.keyToBundlesCache.GetCachedBundleList(msg.Address);
                bool cached = bundles != null;
                if (bundles == null)
                {
                    switch (msg.AddressType)
                    {
                        case DLPResourceManagerMessageLocate.AddressType.AssetPath:
                            {
                                string guidString = AssetDatabase.AssetPathToGUID(msg.Address);
                                GUID guid = new GUID(guidString);
                                bundles = c.builder.GetBundlesForGUID(guid);
                            }
                            break;
                        case DLPResourceManagerMessageLocate.AddressType.BundleName:
                            {
                                bundles = c.builder.GetBundlesForBundleName(msg.Address);
                            }
                            break;
                        case DLPResourceManagerMessageLocate.AddressType.GUID:
                            {
                                GUID guid = new GUID(msg.Address);
                                bundles = c.builder.GetBundlesForGUID(guid);
                            }
                            break;
                        default:
                            {
                                return "ERROR";
                            }
                    }
                    c.keyToBundlesCache.SetCachedBundleList(msg.Address, bundles);
                }

                if (m_DebugPrints)
                {
                    string listOfBundles = string.Join("\n", Array.ConvertAll(bundles, x => x.ToString()));
                    Debug.LogFormat("Bundles Requested by {0}, address={1}, cached={2}, results=\n{3}", msg.AddressType.ToString(), msg.Address, cached.ToString(), listOfBundles);
                }


                RuntimeBundleIdentfier[] converted = Array.ConvertAll(bundles, x => new RuntimeBundleIdentfier(x.BundleName, x.VersionHash));
                return new DLPResourceManagerMessageLocate.Result(converted);
            }
            if (msgObject is HotReloadCheckMessage.Query)
            {
                var msg = (HotReloadCheckMessage.Query)msgObject;
                var result = new HotReloadCheckMessage.Result();
                result.ChangedBundleNames = c.builder.GetStaleBundles(msg.Index, out result.NewIndex);
                if (m_DebugPrints && result.ChangedBundleNames.Length > 0)
                {
                    string listOfBundles = string.Join("\n", Array.ConvertAll(result.ChangedBundleNames, x => x.ToString()));
                    Debug.LogFormat("HotReloadChecki: \n{0}", listOfBundles);
                }
                return result;
            }
            if (msgObject is HotReloadDataMessage.Query)
            {
                var msg = (HotReloadDataMessage.Query)msgObject;
                var result = new HotReloadDataMessage.Result();
                result.Results = Array.ConvertAll(msg.Locators, (x) => (DLPResourceManagerMessageLocate.Result)DataRequestObject(session, x));
                return result;
            }
            return string.Format("Unknown Message Type {0}", msgObject.GetType());
        }

        public byte[] DataRequest(Session session, byte[] message)
        {
            object msgObject = MessageUtil.Deserialize(message);
            object result = DataRequestObject(session, msgObject);
            return MessageUtil.Serialize(result);
        }
    }
}
#endif
