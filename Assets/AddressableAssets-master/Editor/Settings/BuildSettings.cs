using System;
using UnityEditor.Experimental.Build.AssetBundle;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public partial class AddressableAssetSettings
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        [Serializable]
        public class BuildSettings
        {
            /// <summary>
            /// TODO - doc
            /// </summary>
            public enum DeduplicationMode
            {
                None,
                Asset,
                Object,
            }
            /// <summary>
            /// TODO - doc
            /// </summary>
            public DeduplicationMode deduplicationMode = DeduplicationMode.None;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool aggressiveObjectDepude = false;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool useCache = false;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool postProfilerEvents = true;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public BuildCompression compression = BuildCompression.DefaultLZ4;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool appendBuildTargetToBundlePaths = true;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public string bundleBuildPath = "Temp/AddressableAssetsBundles";  //where to build bundles, this is usually a temporary folder (or a folder in the project).  bundles are copied out of this location to their final destination
            /// <summary>
            /// TODO - doc
            /// </summary>
            public int sharedBundleTargetGroup;   //where to copy after build

            /// <summary>
            /// TODO - doc
            /// </summary>
            public ResourceManagerRuntimeData.ProviderMode resourceProviderMode = ResourceManagerRuntimeData.ProviderMode.VirtualMode;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public int localLoadSpeed = 1024 * 1024 * 10;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public int remoteLoadSpeed = 1024 * 1024 * 1;
            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool forceRebuildData = false;


            /// <summary>
            /// TODO - doc
            /// </summary>
            public bool downloadRemoteCatalog = false;

            /// <summary>
            /// TODO - doc
            /// </summary>
            public ProfileSettings.ProfileValue remoteCatalogLocation = new ProfileSettings.ProfileValue();

            /// <summary>
            /// TODO - doc
            /// </summary>
            public ProfileSettings.ProfileValue remoteCatalogProvider = new ProfileSettings.ProfileValue();

            /// <summary>
            /// TODO - doc
            /// </summary>
            public ProfileSettings.ProfileValue remoteCatalogBuildLocation = new ProfileSettings.ProfileValue();

        }
    }
}
