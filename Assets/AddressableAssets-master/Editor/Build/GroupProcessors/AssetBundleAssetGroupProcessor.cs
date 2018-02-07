using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor;
using ResourceManagement.ResourceProviders;
using System.IO;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public abstract class AssetBundleAssetGroupProcessor : AssetGroupProcessor
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public enum BundleMode
        {
            PackTogether,
            PackSeparately
        }
        protected abstract string GetBuildPath(AddressableAssetSettings settings);
        protected abstract string GetBundleLoadPath(AddressableAssetSettings settings, string bundleName);
        protected abstract string GetBundleLoadProvider(AddressableAssetSettings settings);
        protected string GetAssetLoadProvider(AddressableAssetSettings settings)
        {
            return typeof(BundledAssetProvider).FullName;
        }

        protected abstract BundleMode GetBundleMode(AddressableAssetSettings settings);

        internal override void CreateResourceLocationData(
            AddressableAssetSettings settings,
            ResourceManagerRuntimeData runtimeData,
            AddressableAssetSettings.AssetGroup assetGroup,
            string bundleName,
            List<GUID> assetsInBundle,
            Dictionary<GUID, List<string>> assetsToBundles,
            List<ResourceLocationData> locations)
        {
            locations.Add(new ResourceLocationData(bundleName, string.Empty, GetBundleLoadPath(settings, bundleName), GetBundleLoadProvider(settings), ResourceLocationData.LocationType.String, 0, typeof(UnityEngine.AssetBundle).FullName, null));

            foreach (var a in assetsInBundle)
            {
                var assetEntry = settings.FindAssetEntry(a.ToString());
                if (assetEntry == null)
                    continue;
                runtimeData.contentCatalog.locations.Add(new ResourceLocationData(assetEntry.address, assetEntry.guid, assetEntry.assetPath, GetAssetLoadProvider(settings), ResourceLocationData.LocationType.String, settings.labelTable.GetMask(assetEntry.labels), AssetDatabase.GetMainAssetTypeAtPath(assetEntry.assetPath).FullName, assetsToBundles[a].ToArray()));
            }
        }

        internal override void ProcessGroup(AddressableAssetSettings settings, AddressableAssetSettings.AssetGroup assetGroup, List<BuildInput.Definition> bundleInputDefs, List<ResourceLocationData> locationData)
        {
            if (GetBundleMode(settings) == BundleMode.PackTogether)
            {
                var allEntries = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
                foreach (var a in assetGroup.entries)
                    a.GatherAllAssets(allEntries, settings);
                GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.name, "all");
            }
            else
            {
                foreach (var a in assetGroup.entries)
                {
                    var allEntries = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
                    a.GatherAllAssets(allEntries, settings);
                    GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.name, a.address);
                }
            }
        }

        internal override void PostProcessBundles(AddressableAssetSettings settings, AddressableAssetSettings.AssetGroup assetGroup, UnityEditor.Build.AssetBundle.BuildResultInfo buildResult, ResourceManagerRuntimeData runtimeData)
        {
            var path = GetBuildPath(settings);
            if (string.IsNullOrEmpty(path))
                return;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            foreach (var b in buildResult.bundleResults)
            {
                var bundleName = b.Key;
                var targetPath = Path.Combine(path, bundleName);
                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(Path.Combine(settings.buildSettings.bundleBuildPath, bundleName), targetPath, true);
            }
        }

        private void GenerateBuildInputDefinitions(List<AddressableAssetSettings.AssetGroup.AssetEntry> allEntries, List<BuildInput.Definition> buildInputDefs, string groupName, string address)
        {
            var scenes = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
            var assets = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
            foreach (var e in allEntries)
            {
                if (e.assetPath.EndsWith(".unity"))
                    scenes.Add(e);
                else
                    assets.Add(e);
            }
            if (assets.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(assets, groupName + "_assets_" + address + ".bundle"));
            if (scenes.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(scenes, groupName + "_scenes_" + address + ".bundle"));
        }

        private BuildInput.Definition GenerateBuildInputDefinition(List<AddressableAssetSettings.AssetGroup.AssetEntry> assets, string name)
        {
            var assetsInputDef = new BuildInput.Definition();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            var assetIds = new List<AssetIdentifier>(assets.Count);
            foreach (var a in assets)
            {
                var id = new AssetIdentifier();
                id.address = a.assetPath;
                id.asset = new GUID(a.guid);
                assetIds.Add(id);
            }
            assetsInputDef.explicitAssets = assetIds.ToArray();
            return assetsInputDef;
        }
    }
}
