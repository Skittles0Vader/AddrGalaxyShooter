using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class AssetDependency : ADataConverter<GUID, BuildSettings, AssetLoadInfo>
    {
        public override uint Version { get { return 1; } }

        public AssetDependency(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public static bool ValidAsset(GUID asset)
        {
            // TODO: Maybe move this to AssetDatabase or Utility class?
            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path) || path.EndsWith(".unity") || !File.Exists(path))
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

        public override BuildPipelineCodes Convert(GUID asset, BuildSettings settings, out AssetLoadInfo output)
        {
            StartProgressBar("Calculating Asset Dependencies", 2);

            if (!ValidAsset(asset))
            {
                output = new AssetLoadInfo();
                EndProgressBar();
                return BuildPipelineCodes.Error;
            }

            Hash128 hash = CalculateInputHash(asset, settings);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out output))
            {
                if (!EndProgressBar())
                    return BuildPipelineCodes.Canceled;
                return BuildPipelineCodes.SuccessCached;
            }

            output = new AssetLoadInfo();
            output.asset = asset;

            if (!UpdateProgressBar("Calculating included objects"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }
            output.includedObjects = new List<ObjectIdentifier>(BundleBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, settings.target));

            if (!UpdateProgressBar("Calculating referenced objects"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }
            output.referencedObjects = new List<ObjectIdentifier>(BundleBuildInterface.GetPlayerDependenciesForObjects(output.includedObjects.ToArray(), settings.target, settings.typeDB));

            if (UseCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache AssetDependency results for asset '{0}'.", asset);

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }
    }
}
