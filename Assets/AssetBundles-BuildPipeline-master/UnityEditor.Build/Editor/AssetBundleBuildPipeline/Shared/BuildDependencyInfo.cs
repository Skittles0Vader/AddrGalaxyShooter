using System;
using System.Collections.Generic;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildDependencyInfo
    {
        // AssetLoadInfo for all assets
        public Dictionary<GUID, AssetLoadInfo> assetInfo = new Dictionary<GUID, AssetLoadInfo>();
        public Dictionary<GUID, SceneDependencyInfo> sceneInfo = new Dictionary<GUID, SceneDependencyInfo>();
        public Dictionary<GUID, string> sceneAddress = new Dictionary<GUID, string>();

        // Usage Tags
        public BuildUsageTagGlobal buildGlobalUsage = new BuildUsageTagGlobal();
        public Dictionary<GUID, BuildUsageTagSet> sceneUsageTags = new Dictionary<GUID, BuildUsageTagSet>();

        // Lookup maps for fast dependency calculation
        public Dictionary<GUID, List<string>> assetToBundles = new Dictionary<GUID, List<string>>();
        public Dictionary<string, List<GUID>> bundleToAssets = new Dictionary<string, List<GUID>>();

        // Virtual assets for advanced object deduplication
        public HashSet<GUID> virtualAssets = new HashSet<GUID>();
        public Dictionary<ObjectIdentifier, GUID> objectToVirtualAsset = new Dictionary<ObjectIdentifier, GUID>();
    }
}
