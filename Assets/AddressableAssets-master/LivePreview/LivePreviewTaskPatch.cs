#if LIVE_PREVIEW
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AssetBundlePatching;

namespace AddressableAssets.LivePreview
{
    public partial class LivePreviewRuntimeManager
    {
        private class LivePreviewTaskPatch : LivePreviewTask
        {
            private int m_PatchIndex;
            private MessageResponseHandle m_PatchQueryHandle;

            internal LivePreviewTaskPatch(LivePreviewRuntimeManager mgr, int patchIndex) : base(mgr)
            {
                m_PatchIndex = patchIndex;
            }

            internal int GetNewCheckIndex()
            {
                var result = (HotReloadCheckMessage.Result)m_PatchQueryHandle.result;
                return result.NewIndex;
            }

            internal override Status Update()
            {
                if (m_mgr.mTaskQueue.IndexOf(this) != 0)
                    return Status.Blocked;

                // Get a list of all the bundles that have changed since the last time we called this function
                if (m_PatchQueryHandle == null)
                {
                    m_PatchQueryHandle = m_mgr.SendMessage(new HotReloadCheckMessage.Query(m_PatchIndex));
                    Debug.Log("SendingPatchMessage");
                }

                if (m_PatchQueryHandle.result != null)
                {
                    Debug.Log("Patch message received");
                    var result = (HotReloadCheckMessage.Result)m_PatchQueryHandle.result;

                    // Do a query on all those bundles to get a full list of what needs to be reloaded
                    // it's possible that a reference from a bundle has been changed so new bundles might need to be loaded
                    if (result.ChangedBundleNames.Length != 0)
                    {
                        // Create a list of bundles that we have loaded and the query for what needs to be reloaded
                        List<DLPResourceManagerMessageLocate.Query> queries = new List<DLPResourceManagerMessageLocate.Query>();
                        foreach (string bundleName in result.ChangedBundleNames)
                            if (m_mgr.mBundleInfos.ContainsKey(bundleName))
                                queries.Add(new DLPResourceManagerMessageLocate.Query(bundleName, DLPResourceManagerMessageLocate.AddressType.BundleName));
                        var hotReloadMsg = new HotReloadDataMessage.Query();
                        hotReloadMsg.Locators = queries.ToArray();
                        var hrlResult = (HotReloadDataMessage.Result)m_mgr.SendMessageSync(hotReloadMsg); // this could be changed to async, to reduce hitching during patching

                        // We now have all the bundle identifiers for the hot reload.
                        // Create a list of the ones that aren't already loaded
                        HashSet<RuntimeBundleIdentfier> allRequredIds = new HashSet<RuntimeBundleIdentfier>();

                        foreach (DLPResourceManagerMessageLocate.Result r in hrlResult.Results)
                            foreach (RuntimeBundleIdentfier id in r.Bundles)
                                allRequredIds.Add(id);

                        RuntimeBundleIdentfier[] ids = allRequredIds.ToArray();
                        PatchBundlesIfLoaded(ids);
                        LoadBundlesIfNotLoaded(ids);
                    }
                    return Status.Complete;
                }
                return Status.InProgress;
            }
            private bool LoadBundlesIfNotLoaded(RuntimeBundleIdentfier[] ids)
            {
                foreach (RuntimeBundleIdentfier id in ids)
                {
                    if (!m_mgr.mBundleInfos.ContainsKey(id.BundleName))
                    {
                        string bundlePath = GetBundlePath(id);
                        BundleLoadInfo info = m_mgr.mBundleInfos[id.BundleName] = new BundleLoadInfo(id);
                        info.Bundle = AssetBundle.LoadFromFile(bundlePath);
                        if (info.Bundle == null)
                        {
                            Debug.LogErrorFormat("Failed Loading Bundle {0}", bundlePath);
                            return false;
                        }
                    }
                }
                return true;
            }

            private void PatchBundlesIfLoaded(RuntimeBundleIdentfier[] ids)
            {
                List<string> toPatchFilenames = new List<string>();
                List<AssetBundle> toPatchBundles = new List<AssetBundle>();
                foreach (var id in ids)
                {
                    BundleLoadInfo info;
                    if (m_mgr.mBundleInfos.TryGetValue(id.BundleName, out info) && id.Version != info.Identifier.Version)
                    {
                        Debug.Assert(info.State == BundleLoadInfo.LoadState.Ready);
                        toPatchFilenames.Add(GetBundlePath(id));
                        toPatchBundles.Add(info.Bundle);
                        info.Identifier = id;
                    }
                }
                if (toPatchFilenames.Count > 0)
                {
                    AssetBundleUtility.PatchAssetBundles(toPatchBundles.ToArray(), toPatchFilenames.ToArray());
                    Debug.LogFormat("PatchBundlesIfLoaded {0} bundles", toPatchFilenames.Count);
                }
            }
        }
    }
}
#endif