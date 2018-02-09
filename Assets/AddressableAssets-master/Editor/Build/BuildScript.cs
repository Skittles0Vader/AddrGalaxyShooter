using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using ResourceManagement.ResourceProviders;
using System;
using UnityEditor.Build.AssetBundle;
using UnityEditor.Experimental.Build.AssetBundle;
using ResourceManagement.ResourceProviders.Simulation;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build;
using AddressableAssets.LivePreview;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public class BuildScript
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
        }

        static void BuildPlayer(BuildPlayerOptions ops)
        {
            PrepareRuntimeData(true, (ops.options & BuildOptions.Development) != BuildOptions.None, (ops.options & BuildOptions.ConnectWithProfiler) != BuildOptions.None, ops.targetGroup, ops.target);
            BuildPipeline.BuildPlayer(ops);
        }

        internal static BuildDependencyInfo PreviewDependencyInfo()
        {
            var aaSettings = AddressableAssetSettings.GetDefault(false, false);
            if (aaSettings == null)
                return null;
            SceneManagerState.Record();
            var saveRebuild = aaSettings.buildSettings.forceRebuildData;
            aaSettings.buildSettings.forceRebuildData = true;
            var saveMode = aaSettings.buildSettings.resourceProviderMode;
            aaSettings.buildSettings.resourceProviderMode = ResourceManagerRuntimeData.ProviderMode.VirtualMode;
            var result = PrepareRuntimeData(false, false, false, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), EditorUserBuildSettings.activeBuildTarget);
            aaSettings.buildSettings.forceRebuildData = saveRebuild;
            aaSettings.buildSettings.resourceProviderMode = saveMode;
            return result;
        }

        private static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SceneManagerState.Record();
                PrepareRuntimeData(false, true, true, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), EditorUserBuildSettings.activeBuildTarget);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ResourceManagerRuntimeData.Cleanup();
                VirtualAssetBundleRuntimeData.Cleanup();
                SceneManagerState.Restore();
            }
        }

        static ResourceLocationData[] GenerateSimpleLocations<TProvider>(AddressableAssetSettings aaSettings, bool guidAsId)
        {
            var assetIt = aaSettings.groups.SelectMany(g => g.entries).SelectMany(e => e.ExpandAll(aaSettings));
            return assetIt.Select(a => new ResourceLocationData(a.address, a.guid, guidAsId ? a.guid : a.assetPath, typeof(TProvider).FullName, ResourceLocationData.LocationType.String, aaSettings.labelTable.GetMask(a.labels), AssetDatabase.GetMainAssetTypeAtPath(a.assetPath).FullName, null)).ToArray();
        }

        public static BuildDependencyInfo PrepareRuntimeData(bool isPlayerBuild, bool isDevBuild, bool allowProfilerEvents, BuildTargetGroup targetGroup, BuildTarget target)
        {
            BuildDependencyInfo dependencyInfo = null;

            var aaSettings = AddressableAssetSettings.GetDefault(false, false);
            if (aaSettings == null)
                return dependencyInfo;

            var startTime = Time.realtimeSinceStartup;
            var settingsHash = aaSettings.currentHash.ToString();
            ResourceManagerRuntimeData runtimeData = null;
            if (!aaSettings.buildSettings.forceRebuildData)
            {
                runtimeData = ResourceManagerRuntimeData.LoadFromLibrary(aaSettings.buildSettings.resourceProviderMode.ToString());
                if (runtimeData != null && runtimeData.settingsHash != settingsHash)
                {
                    Debug.LogFormat("AddressableAssetSettings have changed, rebuilding data...{0} != {1}", settingsHash, runtimeData.settingsHash);
                    runtimeData = null;
                }
            }

            if (runtimeData != null)
            {
                if (runtimeData.resourceProviderMode !=  ResourceManagerRuntimeData.ProviderMode.PackedMode)
                    AddAddressableScenesToEditorBuildSettingsSceneList(aaSettings, runtimeData);
                if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.VirtualMode)
                {
                    var virtualBundleData = VirtualAssetBundleRuntimeData.LoadFromLibrary();
                    if (virtualBundleData == null)
                        virtualBundleData = CreateVirtualAssetBundleData(aaSettings, runtimeData);
                    virtualBundleData.Save();
                }
                runtimeData.Save(aaSettings.buildSettings.resourceProviderMode.ToString()); //this saves to streaming assets
                Debug.Log("Processed  " + aaSettings.assetEntries.Count() + " addressable assets in " + (Time.realtimeSinceStartup - startTime) + " secs.");
                return dependencyInfo;
            }

            runtimeData = new ResourceManagerRuntimeData(isPlayerBuild ? ResourceManagerRuntimeData.ProviderMode.PackedMode : aaSettings.buildSettings.resourceProviderMode);
            runtimeData.contentCatalog.labels = aaSettings.labelTable.labelNames;
            runtimeData.profileEvents = allowProfilerEvents && aaSettings.buildSettings.postProfilerEvents;
            if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.FastMode)
            {
                runtimeData.contentCatalog.locations.AddRange(GenerateSimpleLocations<AssetDatabaseProvider>(aaSettings, false));
            }
            else
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
                var typeDB = CompilePlayerScripts(targetGroup, target, isDevBuild, false);

                var allBundleInputDefs = new List<BuildInput.Definition>();
                var bundleToAssetGroup = new Dictionary<string, AddressableAssetSettings.AssetGroup>();
                foreach (var assetGroup in aaSettings.groups)
                {
                    var bundleInputDefs = new List<BuildInput.Definition>();
                    assetGroup.processor.ProcessGroup(aaSettings, assetGroup, bundleInputDefs, runtimeData.contentCatalog.locations);
                    foreach (var bid in bundleInputDefs)
                        bundleToAssetGroup.Add(bid.assetBundleName, assetGroup);
                    allBundleInputDefs.AddRange(bundleInputDefs);
                }

                if (allBundleInputDefs.Count > 0)
                {
                    var buildSettings = BundleBuildPipeline.GenerateBundleBuildSettings(typeDB, target, targetGroup);
                    buildSettings.typeDB = typeDB;
                    var buildInput = new BuildInput();
                    buildInput.definitions = allBundleInputDefs.ToArray();

                    int stepCount = 2;
                    if (aaSettings.buildSettings.deduplicationMode == AddressableAssetSettings.BuildSettings.DeduplicationMode.Asset)
                        stepCount += 2;
                    else if (aaSettings.buildSettings.deduplicationMode == AddressableAssetSettings.BuildSettings.DeduplicationMode.Object)
                        stepCount += 1;
                    if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.PackedMode)
                        stepCount += 4;

                    using (var progressTracker = new UnityEditor.Build.Utilities.BuildProgressTracker(stepCount))
                    {
                        ComputeObjectDependencies(aaSettings.buildSettings.deduplicationMode, aaSettings.buildSettings.aggressiveObjectDepude, buildInput, buildSettings, aaSettings.buildSettings.compression, out dependencyInfo, aaSettings.buildSettings.useCache, progressTracker);

                        var assetGroupToBundle = new Dictionary<AddressableAssetSettings.AssetGroup, List<string>>();
                        GenerateLocationLists(aaSettings, runtimeData, bundleToAssetGroup, assetGroupToBundle, dependencyInfo);
                        if (runtimeData.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.PackedMode)
                        {
                            var bundleResults = BuildBundles(dependencyInfo, buildSettings, aaSettings.buildSettings.bundleBuildPath, aaSettings.buildSettings.compression, aaSettings.buildSettings.useCache, progressTracker);
                            foreach (var assetGroup in aaSettings.groups)
                            {
                                List<string> bundles;
                                if (assetGroupToBundle.TryGetValue(assetGroup, out bundles))
                                {
                                    assetGroup.processor.PostProcessBundles(aaSettings, assetGroup, bundleResults, runtimeData);
                                }
                            }
                        }
                        else
                        {
                            var virtualBundleRuntimeData = CreateVirtualAssetBundleData(aaSettings, runtimeData);
                            virtualBundleRuntimeData.Save();
                        }
                    }
                }
            }

            if (runtimeData.resourceProviderMode != ResourceManagerRuntimeData.ProviderMode.PackedMode)
                AddAddressableScenesToEditorBuildSettingsSceneList(aaSettings, runtimeData);

            runtimeData.settingsHash = settingsHash;
            if (aaSettings.buildSettings.downloadRemoteCatalog)
            {
                runtimeData.downloadCatalogLocation = aaSettings.buildSettings.remoteCatalogLocation.Evaluate(aaSettings.profileSettings, aaSettings.activeProfile);
                runtimeData.downloadCatalogProvider = aaSettings.buildSettings.remoteCatalogProvider.Evaluate(aaSettings.profileSettings, aaSettings.activeProfile);
                var catalogBuildLocation = aaSettings.buildSettings.remoteCatalogBuildLocation.Evaluate(aaSettings.profileSettings, aaSettings.activeProfile);
                if (!string.IsNullOrEmpty(catalogBuildLocation))
                {
                    if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(catalogBuildLocation)))
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(catalogBuildLocation));
                    System.IO.File.WriteAllText(catalogBuildLocation, JsonUtility.ToJson(runtimeData.contentCatalog));
                }
            }
            else
            {
                runtimeData.downloadCatalogLocation = runtimeData.downloadCatalogProvider = string.Empty;
            }

            runtimeData.Save(aaSettings.buildSettings.resourceProviderMode.ToString());

            Debug.Log("Processed  " + aaSettings.assetEntries.Count() + " addressable assets in " + (Time.realtimeSinceStartup - startTime) + " secs.");

            return dependencyInfo;
        }

        static private void GenerateLocationLists(AddressableAssetSettings settings, ResourceManagerRuntimeData runtimeData, Dictionary<string, AddressableAssetSettings.AssetGroup> bundleToAssetGroup, Dictionary<AddressableAssetSettings.AssetGroup, List<string>> assetGroupToBundle, BuildDependencyInfo buildInfo)
        {
            foreach (var kvp in buildInfo.bundleToAssets)
            {
                AddressableAssetSettings.AssetGroup assetGroup = null;
                if (!bundleToAssetGroup.TryGetValue(kvp.Key, out assetGroup))
                {
                    if (settings.buildSettings.sharedBundleTargetGroup > 0)//group 0 is the playerdata group
                        assetGroup = settings.groups[settings.buildSettings.sharedBundleTargetGroup];
                    if (assetGroup == null)
                        assetGroup = settings.DefaultGroup;
                }
                if (assetGroup != null)
                {
                    List<string> bundles;
                    if (!assetGroupToBundle.TryGetValue(assetGroup, out bundles))
                        assetGroupToBundle.Add(assetGroup, bundles = new List<string>());
                    bundles.Add(kvp.Key);

                    assetGroup.processor.CreateResourceLocationData(settings,  assetGroup, kvp.Key, kvp.Value, buildInfo.assetToBundles, runtimeData.contentCatalog.locations);
                }
            }
        }

        static internal void ComputeObjectDependencies(AddressableAssetSettings.BuildSettings.DeduplicationMode dedupeMode, bool agressive, BuildInput buildInput, BuildSettings settings, BuildCompression compression, out BuildDependencyInfo dependencyInfo, bool useCache, UnityEditor.Build.Utilities.BuildProgressTracker progress)
        {
            UnityEditor.Build.AssetBundle.Shared.BundleDependencyStep.Build(buildInput, settings, out dependencyInfo, useCache, progress);
            if (dedupeMode == AddressableAssetSettings.BuildSettings.DeduplicationMode.Asset)
            {
                buildInput = DeduplicateAssets(buildInput, settings, dependencyInfo);
                UnityEditor.Build.AssetBundle.Shared.BundleDependencyStep.Build(buildInput, settings, out dependencyInfo, useCache, progress);
            }
            else if (dedupeMode == AddressableAssetSettings.BuildSettings.DeduplicationMode.Object)
            {
                new SharedObjectProcessor(useCache, progress).Convert(dependencyInfo, settings, agressive, out dependencyInfo);
            }
        }

        static private BuildResultInfo BuildBundles(BuildDependencyInfo buildDependencyInfo, BuildSettings buildSettings, string bundleBuildPath, BuildCompression compression, bool useCache, UnityEditor.Build.Utilities.BuildProgressTracker progress)
        {
            BuildWriteInfo cmdSet;
            UnityEditor.Build.AssetBundle.Shared.BundlePackingStep.Build(buildDependencyInfo, out cmdSet, useCache, progress);
            foreach (var b in buildDependencyInfo.bundleToAssets)
            {
                var bundleFolder = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(bundleBuildPath, b.Key));
                if (!System.IO.Directory.Exists(bundleFolder))
                    System.IO.Directory.CreateDirectory(bundleFolder);
            }
            BuildResultInfo result;
            UnityEditor.Build.AssetBundle.Shared.BundleWritingStep.Build(buildSettings, compression, bundleBuildPath, buildDependencyInfo, cmdSet, out result, useCache, progress);
            return result;
        }

        static private BuildInput DeduplicateAssets(BuildInput input, BuildSettings settings, BuildDependencyInfo dependencyInfo)
        {
            var newBuildInput = new BuildInput();
            var allReferencedObjects = new HashSet<ObjectIdentifier>();
            var dupedReferencedObjects = new HashSet<ObjectIdentifier>();
            var referencedObjectsPerBundle = new Dictionary<string, HashSet<ObjectIdentifier>>();
            var includedObjectsPerBundle = new Dictionary<string, HashSet<ObjectIdentifier>>();

            foreach (var bundleDef in input.definitions)
            {
                var includedObjects = new HashSet<ObjectIdentifier>();
                foreach (var includedAsset in bundleDef.explicitAssets)
                    foreach (var obj in dependencyInfo.assetInfo[includedAsset.asset].includedObjects)
                        includedObjects.Add(obj);

                includedObjectsPerBundle.Add(bundleDef.assetBundleName, includedObjects);
            }

            foreach (var bundleDef in input.definitions)
            {
                var includedObjects = includedObjectsPerBundle[bundleDef.assetBundleName];
                var referencedObjects = new HashSet<ObjectIdentifier>();
                foreach (var explicitAsset in bundleDef.explicitAssets)
                {
                    foreach (var dep in dependencyInfo.assetInfo[explicitAsset.asset].referencedObjects)
                    {
                        bool included = false;
                        foreach (var i in includedObjectsPerBundle)
                        {
                            if (i.Value.Contains(dep))
                            {
                                included = true;
                                break;
                            }
                        }
                        if (!included)
                            referencedObjects.Add(dep);
                    }
                }
                referencedObjectsPerBundle.Add(bundleDef.assetBundleName, referencedObjects);
            }

            foreach (var b in referencedObjectsPerBundle)
            {
                foreach (var refObj in b.Value)
                    if (!allReferencedObjects.Add(refObj))
                        dupedReferencedObjects.Add(refObj);
            }

            Dictionary<int, HashSet<GUID>> bundleSets = new Dictionary<int, HashSet<GUID>>();
            foreach (var d in dupedReferencedObjects)
            {
                //create a hash of all the bundles that reference this duplicated asset
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var bundleDef in input.definitions)
                {
                    if (referencedObjectsPerBundle[bundleDef.assetBundleName].Contains(d))
                        sb.Append(bundleDef.assetBundleName);
                }
                var hash = sb.ToString().GetHashCode();

                //add the guid for each asset that is referenced by this unique combination of bundles
                HashSet<GUID> assets;
                if (!bundleSets.TryGetValue(hash, out assets))
                    bundleSets.Add(hash, assets = new HashSet<GUID>());
                assets.Add(d.guid);
            }

            var definitions = new List<BuildInput.Definition>(input.definitions);

            foreach (var pg in bundleSets)
            {
                var assets = new List<AssetIdentifier>();
                //collect all asset names to put in shared bundles
                foreach (var asset in pg.Value)
                {
                    var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
                    if (!AddressablesUtility.IsPathValidForEntry(path))
                        continue;
                    var assetId = new AssetIdentifier();
                    assetId.address = path;
                    assetId.asset = asset;
                    assets.Add(assetId);
                }
                if (assets.Count > 0)
                {
                    var def = new BuildInput.Definition();
                    def.assetBundleName = "shared_" + Mathf.Abs(pg.Key) + ".bundle";
                    def.explicitAssets = assets.ToArray();
                    definitions.Add(def);
                }
            }
            newBuildInput.definitions = definitions.ToArray();

            return newBuildInput;
        }

        internal static VirtualAssetBundleRuntimeData CreateVirtualAssetBundleData(AddressableAssetSettings aaSettings, ResourceManagerRuntimeData runtimeData)
        {
            var virtualBundleData = new VirtualAssetBundleRuntimeData(aaSettings.buildSettings.localLoadSpeed, aaSettings.buildSettings.remoteLoadSpeed);
            var bundledAssets = new Dictionary<string, List<string>>();
            foreach (var loc in runtimeData.contentCatalog.locations)
            {
                if (loc.provider == typeof(BundledAssetProvider).FullName)
                {
                    if (loc.dependencies == null || loc.dependencies.Length == 0)
                        continue;
                    foreach (var dep in loc.dependencies)
                    {
                        List<string> assetsInBundle = null;
                        if (!bundledAssets.TryGetValue(dep, out assetsInBundle))
                            bundledAssets.Add(dep, assetsInBundle = new List<string>());
                        assetsInBundle.Add(loc.id);
                    }
                }
            }

            foreach (var bd in bundledAssets)
            {
                var bundleLocData = runtimeData.contentCatalog.locations.Find(a => a.address == bd.Key);
                var size = bd.Value.Count * 1024 * 1024; //for now estimate 1MB per entry
                virtualBundleData.simulatedAssetBundles.Add(new VirtualAssetBundle(bundleLocData.id, bundleLocData.provider == typeof(LocalAssetBundleProvider).FullName, size, bd.Value));
            }

            return virtualBundleData;
        }

        static UnityEditor.Experimental.Build.Player.TypeDB CompilePlayerScripts(BuildTargetGroup group, BuildTarget target, bool dev, bool assert)
        {
            try
            {
                var compileSettings = new UnityEditor.Experimental.Build.Player.ScriptCompilationSettings();
                compileSettings.target = target;
                compileSettings.group = group;
                compileSettings.options = UnityEditor.Experimental.Build.Player.ScriptCompilationOptions.None;
                if (dev)
                    compileSettings.options |= UnityEditor.Experimental.Build.Player.ScriptCompilationOptions.DevelopmentBuild;
                if (assert)
                    compileSettings.options |= UnityEditor.Experimental.Build.Player.ScriptCompilationOptions.Assertions;

                return UnityEditor.Experimental.Build.Player.PlayerBuildInterface.CompilePlayerScripts(compileSettings, FileUtil.GetUniqueTempPathInProject()).typeDB;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private static void AddAddressableScenesToEditorBuildSettingsSceneList(AddressableAssetSettings settings, ResourceManagerRuntimeData runtimeData)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            //scenes.AddRange(EditorBuildSettings.scenes);
            var sceneEntries = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
            settings.GetAllSceneEntries(sceneEntries);
            foreach (var entry in sceneEntries)
                scenes.Add(new EditorBuildSettingsScene(new GUID(entry.guid), true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        static public HashSet<string> ExtractDuplicates(AddressableAssetSettings settings, List<AddressableAssetSettings.AssetGroup> groups)
        {
            var results = new HashSet<string>();
            var target = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
            var typeDB = CompilePlayerScripts(targetGroup, target, false, false);

            var allBundleInputDefs = new List<BuildInput.Definition>();
            var bundleToAssetGroup = new Dictionary<string, AddressableAssetSettings.AssetGroup>();
            var locations = new List<ResourceLocationData>();
            foreach (var assetGroup in groups)
            {
                var bundleInputDefs = new List<BuildInput.Definition>();
                assetGroup.processor.ProcessGroup(settings, assetGroup, bundleInputDefs, locations);
                foreach (var bid in bundleInputDefs)
                    bundleToAssetGroup.Add(bid.assetBundleName, assetGroup);
                allBundleInputDefs.AddRange(bundleInputDefs);
            }

            if (allBundleInputDefs.Count > 0)
            {
                var buildSettings = BundleBuildPipeline.GenerateBundleBuildSettings(typeDB, target, targetGroup);
                buildSettings.typeDB = typeDB;
                var buildInput = new BuildInput();
                buildInput.definitions = allBundleInputDefs.ToArray();
                BuildDependencyInfo dependencyInfo;
                ComputeObjectDependencies(AddressableAssetSettings.BuildSettings.DeduplicationMode.Asset, false, buildInput, buildSettings, settings.buildSettings.compression, out dependencyInfo, settings.buildSettings.useCache, null);
                foreach (var a in dependencyInfo.assetToBundles)
                {
                    //var path = AssetDatabase.GUIDToAssetPath(a.Key.ToString());
                    var bundle = a.Value[0];
                    if (bundle.StartsWith("shared_"))
                        results.Add(a.Key.ToString());
                }
            }
            return results;
        }
    }
}
