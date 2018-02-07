using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

using BundleCRCMap = System.Collections.Generic.Dictionary<string, uint>;
using System.Linq;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ResourceFileArchiver : ADataConverter<BuildResultInfo, BuildCompression, string, BundleCRCMap>
    {
        public override uint Version { get { return 1; } }

        public ResourceFileArchiver(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(List<ResourceFile> resourceFiles, BuildCompression compression)
        {
            if (!UseCache)
                return new Hash128();

            var fileHashes = new List<string>();
            foreach (var file in resourceFiles)
                fileHashes.Add(HashingMethods.CalculateFileMD5Hash(file.fileName).ToString());
            return HashingMethods.CalculateMD5Hash(Version, fileHashes, compression);
        }

        public override BuildPipelineCodes Convert(BuildResultInfo resultInfo, BuildCompression compression, string outputFolder, out BundleCRCMap output)
        {
            StartProgressBar("Archiving Resource Files", resultInfo.bundleResults.Count);
            output = new BundleCRCMap();

            foreach (var bundle in resultInfo.bundleResults)
            {
                if (!UpdateProgressBar(string.Format("Bundle: {0}", bundle.Key)))
                {
                    EndProgressBar();
                    return BuildPipelineCodes.Canceled;
                }

                var resourceFiles = new List<ResourceFile>(bundle.Value.SelectMany(x => x.resourceFiles));

                var filePath = string.Format("{0}/{1}", outputFolder, bundle.Key);

                uint crc;
                Hash128 hash = CalculateInputHash(resourceFiles, compression);
                if (UseCache && TryLoadFromCache(hash, outputFolder, out crc))
                {
                    output[filePath] = crc;
                    continue;
                }

                crc = BundleBuildInterface.ArchiveAndCompress(resourceFiles.ToArray(), filePath, compression);
                output[filePath] = crc;

                if (UseCache && !TrySaveToCache(hash, bundle.Key, crc, outputFolder))
                    BuildLogger.LogWarning("Unable to cache ResourceFileArchiver result for bundle {0}.", bundle.Key);
            }

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private bool TryLoadFromCache(Hash128 hash, string outputFolder, out uint output)
        {
            string rootCachePath;
            string[] artifactPaths;

            if (!BuildCache.TryLoadCachedResultsAndArtifacts(hash, out output, out artifactPaths, out rootCachePath))
                return false;

            Directory.CreateDirectory(outputFolder);

            foreach (var artifact in artifactPaths)
                File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
            return true;
        }

        private bool TrySaveToCache(Hash128 hash, string filePath, uint output, string outputFolder)
        {
            return BuildCache.SaveCachedResultsAndArtifacts(hash, output, new[] { filePath }, outputFolder);
        }
    }
}
