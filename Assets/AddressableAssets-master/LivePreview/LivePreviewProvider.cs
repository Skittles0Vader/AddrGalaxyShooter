#if LIVE_PREVIEW
using System;
using ResourceManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement.Util;

#if false //This goes in the BuildScript... at the beginning
            if (datalessPlayer)
            {
                GameObject livePreviewObj = new GameObject("LivePreviewRuntimeManager");
                LivePreviewRuntimeManager mgr = livePreviewObj.AddComponent<LivePreviewRuntimeManager>();
                mgr.SessionId = sessionId;

                LivePreviewProvider prov = new LivePreviewProvider(mgr);
                ResourceManager.resourceProviders.Insert(0, new CachedProvider(prov));
                return;
            }
#endif


namespace AddressableAssets.LivePreview
{
    public class LivePreviewProvider : IResourceProvider
    {
        LivePreviewRuntimeManager m_LivePreviewManager;

        public LivePreviewProvider(LivePreviewRuntimeManager mgr)
        {
            m_LivePreviewManager = mgr;
        }

        public string providerId { get { return typeof(LivePreviewProvider).FullName; } }

        public bool CanProvide<T>(IResourceLocation loc) where T : class
        {
            return loc.providerId == providerId;
        }
        
        public IAsyncOperation<TObject> ProvideAsync<TObject>(IResourceLocation loc, IAsyncOperation<IList<object>> loadDependencyOperation) where TObject : class
        {
            Debug.Log(string.Format("DynamicResourceManagerProvider::ProvideAsync {0}", loc.id));
            int startFrame = Time.frameCount;
            
            IAsyncOperation<TObject> asyncObj = m_LivePreviewManager.LoadAssetByGUIDAsync<TObject>(loc.id);
            asyncObj.completed += (obj)=> ResourceManagerEventCollector.PostEvent(ResourceManagerEventCollector.EventType.LoadAsyncCompletion, loc, Time.frameCount - startFrame);
            return asyncObj;
        }

        public bool Release(IResourceLocation loc, object asset)
        {
            return m_LivePreviewManager.Release(loc.id);
        }
    }
}
#endif