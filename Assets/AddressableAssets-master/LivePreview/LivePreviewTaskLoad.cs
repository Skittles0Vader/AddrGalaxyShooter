#if LIVE_PREVIEW
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AssetBundlePatching;

namespace AddressableAssets.LivePreview
{
    public partial class LivePreviewRuntimeManager
    {
        private class LivePreviewTaskLoad : LivePreviewTask
        {
            internal enum LoadState
            {
                Start,
                FetchingBundleList,
                WaitingForDependencies,
                WaitingForBundleLoading,
                LoadingObject,
                Done
            }

            internal LoadState State;
            private string m_assetName;
            private Type m_loadType;

            internal List<BundleLoadInfo> BundleInfos = new List<BundleLoadInfo>();
            internal MessageResponseHandle BundleListFetchHandle;
            internal AssetBundleRequest ObjectRequest;

            internal LivePreviewTaskLoad(LivePreviewRuntimeManager mgr, string assetName, Type loadType) : base(mgr)
            {
                m_assetName = assetName;
                m_loadType = loadType;
            }

            internal delegate void OnCompleteDelegate(object result);
            internal event OnCompleteDelegate OnComplete;

            internal void TriggerObjectCompleted(object result)
            {
                State = LoadState.Done;
                if (OnComplete != null)
                    OnComplete(result);
            }

            internal override Status Update()
            {
                if (State == LoadState.Start)
                {
                    BundleListFetchHandle = m_mgr.SendMessage(new DLPResourceManagerMessageLocate.Query(m_assetName, DLPResourceManagerMessageLocate.AddressType.GUID));
                    State = LoadState.FetchingBundleList;
                }
                if (State == LoadState.FetchingBundleList)
                {
                    if (BundleListFetchHandle.result != null)
                        State = LoadState.WaitingForDependencies;
                }
                if (State == LoadState.WaitingForDependencies)
                {
                    var result = (DLPResourceManagerMessageLocate.Result)BundleListFetchHandle.result;
                    if (m_mgr.AreBundleOperationsActive(result.Bundles))
                        return Status.Blocked;

                    foreach (RuntimeBundleIdentfier bundleId in result.Bundles)
                    {
                        if (!m_mgr.mBundleInfos.ContainsKey(bundleId.BundleName))
                            m_mgr.mBundleInfos[bundleId.BundleName] = new BundleLoadInfo(bundleId);

                        BundleLoadInfo info = m_mgr.mBundleInfos[bundleId.BundleName];
                        if (info.State == BundleLoadInfo.LoadState.None)
                        {
                            info = m_mgr.LoadBundleAsync(bundleId);
                        }
                        // bundle is loaded but the version is different
                        else if (bundleId.Version != info.Identifier.Version)
                        {
                            // flag as needing to be patched and set the new id
                            info.State = BundleLoadInfo.LoadState.Patching;
                            info.Identifier = bundleId;
                        }
                        BundleInfos.Add(info);
                    }
                    State = LoadState.WaitingForBundleLoading;
                }
                if (State == LoadState.WaitingForBundleLoading)
                {
                    if (BundleInfos.Count(info => info.State == BundleLoadInfo.LoadState.Loading) == 0)
                    {
                        foreach (BundleLoadInfo info in BundleInfos)
                        {
                            if (info.Bundle == null)
                            {
                                Debug.LogError(string.Format("Failed to load bundle {0}. Path: {1}", info.Identifier.ToString(), GetBundlePath(info.Identifier)));
                                TriggerObjectCompleted(null);
                                return Status.Complete;
                            }
                        }

                        // build list of bundles to patch
                        List<string> filenames = new List<string>();
                        List<AssetBundle> bundles = new List<AssetBundle>();
                        foreach (BundleLoadInfo info in BundleInfos)
                        {
                            if (info.State == BundleLoadInfo.LoadState.Patching)
                            {
                                filenames.Add(GetBundlePath(info.Identifier));
                                bundles.Add(info.Bundle);
                                info.State = BundleLoadInfo.LoadState.Ready;
                            }
                        }
                        if (bundles.Count > 0)
                            AssetBundleUtility.PatchAssetBundles(bundles.ToArray(), filenames.ToArray());

                        // Now load the actual asset
                        ObjectRequest = BundleInfos[0].Bundle.LoadAssetAsync("main", m_loadType);
                        State = LoadState.LoadingObject;
                    }
                }
                if (State == LoadState.LoadingObject)
                {
                    if (ObjectRequest.isDone)
                    {
                        TriggerObjectCompleted(ObjectRequest.asset);
                        return Status.Complete;
                    }
                }
                return Status.InProgress;
            }
        }
    }
}
#endif