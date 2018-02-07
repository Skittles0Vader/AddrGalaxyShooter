using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class AssetBundleBuildConverter : ADataConverter<AssetBundleBuild[], BuildInput>
    {
        public override uint Version { get { return 1; } }

        public AssetBundleBuildConverter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(AssetBundleBuild[] input)
        {
            if (!UseCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(Version, input);
        }

        public override BuildPipelineCodes Convert(AssetBundleBuild[] input, out BuildInput output)
        {
            StartProgressBar(input);

            // If enabled, try loading from cache
            var hash = CalculateInputHash(input);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out output))
            {
                EndProgressBar();
                return BuildPipelineCodes.SuccessCached;
            }

            // Convert inputs
            output = new BuildInput();

            if (input.IsNullOrEmpty())
            {
                BuildLogger.LogError("Unable to continue packing. Input is null or empty!");
                EndProgressBar();
                return BuildPipelineCodes.Error;
            }

            output.definitions = new BuildInput.Definition[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                output.definitions[i].assetBundleName = input[i].assetBundleName;
                output.definitions[i].explicitAssets = new AssetIdentifier[input[i].assetNames.Length];
                for (var j = 0; j < input.Length; j++)
                {
                    UpdateProgressBar(input[i].assetNames[j]);
                    var guid = AssetDatabase.AssetPathToGUID(input[i].assetNames[j]);
                    output.definitions[i].explicitAssets[j].asset = new GUID(guid);
                    if (input[i].addressableNames.IsNullOrEmpty() || input[i].addressableNames.Length <= j || string.IsNullOrEmpty(input[i].addressableNames[j]))
                        output.definitions[i].explicitAssets[j].address = input[i].assetNames[j];
                    else
                        output.definitions[i].explicitAssets[j].address = input[i].addressableNames[j];
                }
            }

            // Cache results
            if (UseCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache AssetBundleBuildConverter results.");

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private void StartProgressBar(AssetBundleBuild[] input)
        {
            if (ProgressTracker == null)
                return;

            var progressCount = 0;
            foreach (var bundle in input)
                progressCount += bundle.assetNames.Length;
            StartProgressBar("Converting AssetBundleBuild data", progressCount);
        }
    }
}
