using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class SceneDependency : ADataConverter<GUID, BuildSettings, BuildUsageTagSet, SceneDependencyInfo>
    {
        public override uint Version { get { return 1; } }

        public SceneDependency(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public static bool ValidScene(GUID asset)
        {
            // TODO: Maybe move this to AssetDatabase or Utility class?
            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity") || !File.Exists(path))
                return false;
            return true;
        }

        private Hash128 CalculateInputHash(GUID asset, BuildSettings settings)
        {
            if (!UseCache)
                return new Hash128();

            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            var assetHash = AssetDatabase.GetAssetDependencyHash(path).ToString();
            var dependencies = AssetDatabase.GetDependencies(path);
            var dependencyHashes = new string[dependencies.Length];
            for (var i = 0; i < dependencies.Length; ++i)
                dependencyHashes[i] = AssetDatabase.GetAssetDependencyHash(dependencies[i]).ToString();

            return HashingMethods.CalculateMD5Hash(Version, assetHash, dependencyHashes, settings);
        }

        private string GetBuildPath(Hash128 hash)
        {
            var path = BundleBuildPipeline.kTempBundleBuildPath;
            if (UseCache)
                path = BuildCache.GetPathForCachedArtifacts(hash);
            Directory.CreateDirectory(path);
            return path;
        }

        public override BuildPipelineCodes Convert(GUID scene, BuildSettings settings, BuildUsageTagSet usageSet, out SceneDependencyInfo output)
        {
            StartProgressBar("Calculating Scene Dependencies", 1);
            if (!ValidScene(scene))
            {
                output = new SceneDependencyInfo();
                EndProgressBar();
                return BuildPipelineCodes.Error;
            }

            var scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());
            if (!UpdateProgressBar(scenePath))
            {
                output = new SceneDependencyInfo();
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            Hash128 hash = CalculateInputHash(scene, settings);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out output) && BuildCache.TryLoadCachedResults(hash, out usageSet))
            {
                if (!EndProgressBar())
                    return BuildPipelineCodes.Canceled;
                return BuildPipelineCodes.SuccessCached;
            }

            output = BundleBuildInterface.PrepareScene(scenePath, settings, usageSet, GetBuildPath(hash));

            if (UseCache && !BuildCache.SaveCachedResults(hash, output) && !BuildCache.SaveCachedResults(hash, usageSet))
                BuildLogger.LogWarning("Unable to cache SceneDependency results for asset '{0}'.", scene);

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }
    }
}
