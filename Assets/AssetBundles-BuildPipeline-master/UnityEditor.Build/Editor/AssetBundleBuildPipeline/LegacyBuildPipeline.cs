using UnityEditor.Build.AssetBundle;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Player;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build
{
    public static class LegacyBuildPipeline
    {
        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var buildInput = BundleBuildInterface.GenerateBuildInput();
            return BuildAssetBundles_Internal(outputPath, buildInput, assetBundleOptions, targetPlatform);
        }

        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildInput buildInput;
            var converter = new AssetBundleBuildConverter(false, null);
            var errorCode = converter.Convert(builds, out buildInput);
            if (errorCode < BuildPipelineCodes.Success)
                return null;

            return BuildAssetBundles_Internal(outputPath, buildInput, assetBundleOptions, targetPlatform);
        }

        internal static AssetBundleManifest BuildAssetBundles_Internal(string outputPath, BuildInput buildInput, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            var playerSettings = PlayerBuildPipeline.GeneratePlayerBuildSettings(targetPlatform);
            ScriptCompilationResult scriptResults;
            var errorCode = PlayerBuildPipeline.BuildPlayerScripts(playerSettings, out scriptResults);
            if (errorCode < BuildPipelineCodes.Success)
                return null;

            var bundleSettings = BundleBuildPipeline.GenerateBundleBuildSettings(scriptResults.typeDB, targetPlatform);

            BuildCompression compression = BuildCompression.DefaultLZMA;
            if ((assetBundleOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0)
                compression = BuildCompression.DefaultLZ4;
            else if ((assetBundleOptions & BuildAssetBundleOptions.UncompressedAssetBundle) != 0)
                compression = BuildCompression.DefaultUncompressed;

            var useCache = (assetBundleOptions & BuildAssetBundleOptions.ForceRebuildAssetBundle) == 0;

            BuildResultInfo result;
            errorCode = BundleBuildPipeline.BuildAssetBundles(buildInput, bundleSettings, compression, outputPath, out result, useCache);
            if (errorCode < BuildPipelineCodes.Success)
                return null;

            // TODO: Unity 5 Manifest
            return null;
        }
    }
}