#if LIVE_PREVIEW

namespace AddressableAssets.LivePreview
{
    partial class LivePreviewRuntimeManager
    {
        private abstract class LivePreviewTask
        {
            internal LivePreviewTask(LivePreviewRuntimeManager mgr)
            {
                m_mgr = mgr;
            }

            internal LivePreviewRuntimeManager m_mgr;
            internal enum Status
            {
                InProgress,
                Blocked,
                Complete
            }

            internal abstract Status Update();
        }
    }
}
#endif