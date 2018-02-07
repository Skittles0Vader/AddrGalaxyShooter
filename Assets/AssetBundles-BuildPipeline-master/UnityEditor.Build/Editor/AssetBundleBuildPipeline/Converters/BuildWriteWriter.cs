using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.AssetBundle.DataTypes;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class BuildWriteWriter : ADataConverter<BuildWriteInfo, BuildSettings, BuildUsageTagGlobal, BuildResultInfo>
    {
        public override uint Version { get { return 1; } }

        public BuildWriteWriter(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(IWriteOperation operation, List<WriteCommand> dependencies, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet buildUsage)
        {
            if (!UseCache)
                return new Hash128();

            var empty = new GUID();
            var assets = new HashSet<GUID>();
            var assetHashes = new List<Hash128>();
            foreach (var objectId in operation.command.serializeObjects)
            {
                var guid = objectId.serializationObject.guid;
                if (guid == empty || !assets.Add(guid))
                    continue;

                var path = AssetDatabase.GUIDToAssetPath(guid.ToString());
                assetHashes.Add(AssetDatabase.GetAssetDependencyHash(path));
            }

            var sceneOp = operation as SceneDataWriteOperation;
            if (sceneOp != null)
                assetHashes.Add(HashingMethods.CalculateFileMD5Hash(sceneOp.processedScene));

            return HashingMethods.CalculateMD5Hash(Version, operation, assetHashes, dependencies, globalUsage, buildUsage, settings);
        }

        private string GetBuildPath(Hash128 hash)
        {
            var path = BundleBuildPipeline.kTempBundleBuildPath;
            if (UseCache)
                path = BuildCache.GetPathForCachedArtifacts(hash);
            Directory.CreateDirectory(path);
            return path;
        }

        public override BuildPipelineCodes Convert(BuildWriteInfo writeInfo, BuildSettings settings, BuildUsageTagGlobal globalUsage, out BuildResultInfo output)
        {
            var allCommands = new List<WriteCommand>(writeInfo.assetBundles.Values.Select(x => x.command));
            allCommands.AddRange(writeInfo.sceneBundles.Values.SelectMany(x => x.Select(y => y.command)));

            StartProgressBar("Writing Serialized Files", allCommands.Count);

            output = new BuildResultInfo();

            int count = 1;
            foreach (var bundle in writeInfo.assetBundles)
            {
                if (!UpdateProgressBar(string.Format("Serialized File: {0} Bundle: {1}", count++, bundle.Key)))
                {
                    EndProgressBar();
                    return BuildPipelineCodes.Canceled;
                }

                List<WriteResult> results;
                output.bundleResults.GetOrAdd(bundle.Key, out results);
                WriteSerialziedFiles(bundle.Key, bundle.Value, allCommands, settings, globalUsage, ref results);
            }

            foreach (var bundle in writeInfo.sceneBundles)
            {
                if (!UpdateProgressBar(string.Format("Serialized File: {0} Bundle: {1}", count++, bundle.Key)))
                {
                    EndProgressBar();
                    return BuildPipelineCodes.Canceled;
                }

                List<WriteResult> results;
                output.bundleResults.GetOrAdd(bundle.Key, out results);
                WriteSerialziedFiles(bundle.Key, bundle.Value, allCommands, settings, globalUsage, ref results);
            }

            // TODO: Write Player Data Serialized Files

            // TODO: Write Raw Serialized Files

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private void WriteSerialziedFiles(string bundleName, List<IWriteOperation> ops, List<WriteCommand> allCommands, BuildSettings settings, BuildUsageTagGlobal globalUsage, ref List<WriteResult> outResults)
        {
            foreach (var op in ops)
                WriteSerialziedFiles(bundleName, op, allCommands, settings, globalUsage, ref outResults);
        }

        private void WriteSerialziedFiles(string bundleName, IWriteOperation op, List<WriteCommand> allCommands, BuildSettings settings, BuildUsageTagGlobal globalUsage, ref List<WriteResult> outResults)
        {
            WriteResult result;
            var dependencies = op.CalculateDependencies(allCommands);
            
            var objectIDs = op.command.serializeObjects.Select(x => x.serializationObject).ToArray();
            var dependentIDs = dependencies.SelectMany(x => x.serializeObjects.Select(y => y.serializationObject)).ToArray();
            
            BuildUsageTagSet buildUsage = new BuildUsageTagSet();
            BundleBuildInterface.CalculateBuildUsageTags(objectIDs, dependentIDs, globalUsage, buildUsage);

            Hash128 hash = CalculateInputHash(op, dependencies, settings, globalUsage, buildUsage);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out result))
            {
                outResults.Add(result);
                return;
            }

            result = op.Write(GetBuildPath(hash), dependencies, settings, globalUsage, buildUsage);
            outResults.Add(result);

            if (UseCache && !BuildCache.SaveCachedResults(hash, result))
                BuildLogger.LogWarning("Unable to cache CommandSetWriter results for command '{0}'.", op.command.internalName);
        }
    }
}
