using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class BuildInputDependency : ADataConverter<BuildInput, BuildSettings, BuildDependencyInfo>
    {
        public override uint Version { get { return 1; } }

        public override bool UseCache
        {
            get
            {
                return base.UseCache;
            }

            set
            {
                m_AssetDependency.UseCache = UseCache;
                m_SceneDependency.UseCache = UseCache;
                base.UseCache = value;
            }
        }


        public BuildInputDependency(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker)
        {
            m_AssetDependency.UseCache = UseCache;
            m_SceneDependency.UseCache = UseCache;
        }

        private AssetDependency m_AssetDependency = new AssetDependency(true, null);
        private SceneDependency m_SceneDependency = new SceneDependency(true, null);

        public override BuildPipelineCodes Convert(BuildInput input, BuildSettings settings, out BuildDependencyInfo output)
        {
            StartProgressBar(input);

            output = new BuildDependencyInfo();
            foreach (var bundle in input.definitions)
            {
                foreach (var asset in bundle.explicitAssets)
                {
                    if (SceneDependency.ValidScene(asset.asset))
                    {
                        if (!UpdateProgressBar(asset.asset))
                        {
                            EndProgressBar();
                            return BuildPipelineCodes.Canceled;
                        }

                        // Get Scene Dependency Information
                        SceneDependencyInfo sceneInfo;
                        BuildUsageTagSet usageTags = new BuildUsageTagSet();
                        BuildPipelineCodes errorCode = m_SceneDependency.Convert(asset.asset, settings, usageTags, out sceneInfo);
                        if (errorCode < BuildPipelineCodes.Success)
                        {
                            EndProgressBar();
                            return errorCode;
                        }

                        // Add generated scene information to BuildDependencyInfo
                        output.sceneInfo.Add(asset.asset, sceneInfo);
                        output.sceneUsageTags.Add(asset.asset, usageTags);
                        output.sceneAddress.Add(asset.asset, asset.address);

                        // Add usage tags to BuildDependencyInfo
                        output.buildGlobalUsage |= sceneInfo.globalUsage;

                        // Add the current bundle as dependency[0]
                        List<string> bundles = new List<string>();
                        bundles.Add(bundle.assetBundleName);
                        output.assetToBundles.Add(asset.asset, bundles);

                        // Add the current asset to the list of assets for a bundle
                        List<GUID> bundleAssets;
                        output.bundleToAssets.GetOrAdd(bundle.assetBundleName, out bundleAssets);
                        bundleAssets.Add(asset.asset);
                    }
                    else if (AssetDependency.ValidAsset(asset.asset))
                    {
                        if (!UpdateProgressBar(asset.asset))
                        {
                            EndProgressBar();
                            return BuildPipelineCodes.Canceled;
                        }

                        // Get Asset Dependency Information
                        AssetLoadInfo assetInfo;
                        BuildPipelineCodes errorCode = m_AssetDependency.Convert(asset.asset, settings, out assetInfo);
                        if (errorCode < BuildPipelineCodes.Success)
                        {
                            EndProgressBar();
                            return errorCode;
                        }

                        // Convert Asset Dependency Information to Asset Load Information
                        assetInfo.address = string.IsNullOrEmpty(asset.address) ? AssetDatabase.GUIDToAssetPath(asset.asset.ToString()) : asset.address;

                        // Add generated asset information to BuildDependencyInfo
                        output.assetInfo.Add(asset.asset, assetInfo);

                        // Add the current bundle as dependency[0]
                        List<string> bundles = new List<string>();
                        bundles.Add(bundle.assetBundleName);
                        output.assetToBundles.Add(asset.asset, bundles);

                        // Add the current asset to the list of assets for a bundle
                        List<GUID> bundleAssets;
                        output.bundleToAssets.GetOrAdd(bundle.assetBundleName, out bundleAssets);
                        bundleAssets.Add(asset.asset);
                    }
                    else
                    {
                        if (!UpdateProgressBar(asset.asset))
                        {
                            EndProgressBar();
                            return BuildPipelineCodes.Canceled;
                        }
                    }
                }
            }

            if (!UpdateProgressBar("Calculating asset to bundle dependencies"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            // Generate the explicit asset to bundle dependency lookup
            foreach (var asset in output.assetInfo.Values)
            {
                var assetBundles = output.assetToBundles[asset.asset];
                foreach (var reference in asset.referencedObjects)
                {
                    List<string> refBundles;
                    if (!output.assetToBundles.TryGetValue(reference.guid, out refBundles))
                        continue;

                    var dependency = refBundles[0];
                    if (assetBundles.Contains(dependency))
                        continue;

                    assetBundles.Add(dependency);
                }
            }

            // Generate the explicit scene to bundle dependency lookup
            foreach (var scene in output.sceneInfo)
            {
                var assetBundles = output.assetToBundles[scene.Key];
                foreach (var reference in scene.Value.referencedObjects)
                {
                    List<string> refBundles;
                    if (!output.assetToBundles.TryGetValue(reference.guid, out refBundles))
                        continue;

                    var dependency = refBundles[0];
                    if (assetBundles.Contains(dependency))
                        continue;

                    assetBundles.Add(dependency);
                }
            }

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private void StartProgressBar(BuildInput input)
        {
            if (ProgressTracker == null)
                return;

            var progressCount = 1;
            foreach (var bundle in input.definitions)
                progressCount += bundle.explicitAssets.Length;
            StartProgressBar("Processing Asset Dependencies", progressCount);
        }

        private bool UpdateProgressBar(GUID guid)
        {
            if (ProgressTracker == null)
                return true;

            var path = AssetDatabase.GUIDToAssetPath(guid.ToString());
            return UpdateProgressBar(path);
        }
    }
}
