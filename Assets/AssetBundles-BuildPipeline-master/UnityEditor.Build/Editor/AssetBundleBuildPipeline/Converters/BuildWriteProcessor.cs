using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Build.AssetBundle.DataTypes;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class BuildWriteProcessor : ADataConverter<BuildDependencyInfo, BuildWriteInfo>
    {
        public const string kUnityDefaultResourcePath = "library/unity default resources";

        public override uint Version { get { return 1; } }

        public BuildWriteProcessor(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(BuildDependencyInfo buildInfo)
        {
            if (!UseCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(Version, buildInfo.assetInfo, buildInfo.sceneInfo, buildInfo.assetToBundles, buildInfo.bundleToAssets);
        }

        public override BuildPipelineCodes Convert(BuildDependencyInfo buildInfo, out BuildWriteInfo writeInfo)
        {
            StartProgressBar("Generating Build Commands", buildInfo.bundleToAssets.Count);

            Hash128 hash = CalculateInputHash(buildInfo);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out writeInfo))
            {
                writeInfo = new BuildWriteInfo();
                EndProgressBar();
                return BuildPipelineCodes.SuccessCached;
            }

            writeInfo = new BuildWriteInfo();
            foreach (var bundle in buildInfo.bundleToAssets)
            {
                if (!UpdateProgressBar("Generating Build Commands"))
                    return BuildPipelineCodes.Canceled;

                // TODO: Handle Player Data & Raw write formats
                if (IsAssetBundle(bundle.Value))
                {
                    var op = CreateAssetBundleWriteOperation(bundle.Key, bundle.Value, buildInfo);
                    writeInfo.assetBundles.Add(bundle.Key, op);
                }
                else if (IsSceneBundle(bundle.Value))
                {
                    var ops = CreateSceneBundleWriteOperations(bundle.Key, bundle.Value, buildInfo);
                    writeInfo.sceneBundles.Add(bundle.Key, ops);
                }
                else
                {
                    BuildLogger.LogError("Bundle '{0}' contains mixed assets and scenes.", bundle.Key);
                }
            }

            if (UseCache && !BuildCache.SaveCachedResults(hash, writeInfo))
                BuildLogger.LogWarning("Unable to cache CommandSetProcessor results.");

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private bool IsAssetBundle(List<GUID> assets)
        {
            foreach (var asset in assets)
            {
                if (AssetDependency.ValidAsset(asset))
                    continue;
                return false;
            }
            return true;
        }

        private bool IsSceneBundle(List<GUID> assets)
        {
            foreach (var asset in assets)
            {
                if (SceneDependency.ValidScene(asset))
                    continue;
                return false;
            }
            return true;
        }

        private IWriteOperation CreateAssetBundleWriteOperation(string bundleName, List<GUID> assets, BuildDependencyInfo buildInfo)
        {
            var dependencies = new HashSet<string>();
            var serializeObjects = new HashSet<ObjectIdentifier>();

            var op = new AssetBundleWriteOperation();
            op.command.fileName = GenerateInternalFileName(bundleName);
            op.command.internalName = string.Format("archive:/{0}/{0}", op.command.fileName);

            op.info.bundleName = bundleName;
            op.info.bundleAssets = new List<AssetLoadInfo>();
            foreach (var asset in assets)
            {
                AssetLoadInfo assetInfo;
                if (!buildInfo.assetInfo.TryGetValue(asset, out assetInfo))
                {
                    BuildLogger.LogWarning("Could not find info for asset '{0}'.", asset);
                    continue;
                }

                op.info.bundleAssets.Add(assetInfo);

                dependencies.UnionWith(buildInfo.assetToBundles[asset]);
                serializeObjects.UnionWith(assetInfo.includedObjects);
                foreach (var reference in assetInfo.referencedObjects)
                {
                    if (reference.filePath == kUnityDefaultResourcePath)
                        continue;

                    if (buildInfo.assetInfo.ContainsKey(reference.guid))
                        continue;

                    serializeObjects.Add(reference);
                }
            }
            dependencies.Remove(bundleName); // Don't include self as dependency

            op.info.bundleDependencies = dependencies.OrderBy(x => x).ToList();
            op.command.dependencies = op.info.bundleDependencies.Select(x => string.Format("archive:/{0}/{0}", GenerateInternalFileName(x))).ToList();
            op.command.serializeObjects = serializeObjects.Select(x => new SerializationInfo
            {
                serializationObject = x,
                serializationIndex = SerializationIndexFromObjectIdentifier(x)
            }).ToList();

            return op;
        }

        private List<IWriteOperation> CreateSceneBundleWriteOperations(string bundleName, List<GUID> scenes, BuildDependencyInfo buildInfo)
        {
            // The 'Folder' we mount asset bundles to is the same as the internal file name of the first file in the archive
            var bundleFileName = GenerateInternalFileName(AssetDatabase.GUIDToAssetPath(scenes[0].ToString()));

            var ops = new List<SceneDataWriteOperation>();
            var sceneLoadInfo = new List<SceneLoadInfo>();
            var dependencies = new HashSet<string>();
            foreach (var scene in scenes)
            {
                var op = CreateSceneDataWriteOperation(bundleName, bundleFileName, scene, buildInfo);
                ops.Add((SceneDataWriteOperation)op);

                var scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());
                sceneLoadInfo.Add(new SceneLoadInfo
                {
                    asset = scene,
                    address = buildInfo.sceneAddress[scene],
                    internalName = GenerateInternalFileName(scenePath)
                });

                dependencies.UnionWith(buildInfo.assetToBundles[scene]);
            }
            dependencies.Remove(bundleName); // Don't include self as dependency

            // First write op must be SceneBundleWriteOperation
            var bundleOp = new SceneBundleWriteOperation(ops[0]);
            ops[0] = bundleOp;
            foreach (var serializeObj in bundleOp.command.serializeObjects)
                serializeObj.serializationIndex++; // Shift by 1 to account for asset bundle object

            bundleOp.info.bundleName = bundleName;
            bundleOp.info.bundleScenes = sceneLoadInfo;
            bundleOp.info.bundleDependencies = dependencies.OrderBy(x => x).ToList();

            return ops.Cast<IWriteOperation>().ToList();
        }

        private IWriteOperation CreateSceneDataWriteOperation(string bundleName, string bundleFileName, GUID scene, BuildDependencyInfo buildInfo)
        {
            var sceneInfo = buildInfo.sceneInfo[scene];

            var op = new SceneDataWriteOperation();
            op.scene = sceneInfo.scene;
            op.processedScene = sceneInfo.processedScene;
            op.command.fileName = GenerateInternalFileName(sceneInfo.scene) + ".sharedAssets";
            // TODO: This is bundle formatted internal name, we need to rethink this for PlayerData
            op.command.internalName = string.Format("archive:/{0}/{1}", bundleFileName, op.command.fileName);
            // TODO: Rethink the way we do dependencies here, assetToBundles is for bundles only, won't work for PlayerData or Raw Data.
            op.command.dependencies = buildInfo.assetToBundles[scene].OrderBy(x => x).Where(x => x != bundleName).Select(x => string.Format("archive:/{0}/{0}", GenerateInternalFileName(x))).ToList();
            buildInfo.sceneUsageTags.TryGetValue(scene, out op.usageTags);
            op.command.serializeObjects = new List<SerializationInfo>();
            op.preloadInfo.preloadObjects = new List<ObjectIdentifier>();
            long identifier = 2; // Scenes use linear id assignment
            foreach (var reference in sceneInfo.referencedObjects)
            {
                if (!buildInfo.assetInfo.ContainsKey(reference.guid) && reference.filePath != kUnityDefaultResourcePath)
                {
                    op.command.serializeObjects.Add(new SerializationInfo
                    {
                        serializationObject = reference,
                        serializationIndex = identifier++
                    });
                }
                else
                    op.preloadInfo.preloadObjects.Add(reference);
            }

            // TODO: Add this functionality:
            // Unique to scenes, we point at sharedAssets of a previously built scene in this set as a dependency to reduce object duplication. 

            return op;
        }

        public static string GenerateInternalFileName(string name)
        {
            var md4 = MD4.Create();
            var bytes = Encoding.ASCII.GetBytes(name);
            md4.TransformFinalBlock(bytes, 0, bytes.Length);
            return "CAB-" + BitConverter.ToString(md4.Hash, 0).ToLower().Replace("-", "");
        }

        public static long SerializationIndexFromObjectIdentifier(ObjectIdentifier objectID)
        {
            byte[] bytes;
            var md4 = MD4.Create();
            if (objectID.fileType == FileType.MetaAssetType || objectID.fileType == FileType.SerializedAssetType)
            {
                // TODO: Variant info
                // NOTE: ToString() required as unity5 used the guid as a string to hash
                bytes = Encoding.ASCII.GetBytes(objectID.guid.ToString());
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
                bytes = BitConverter.GetBytes((int)objectID.fileType);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }
            // Or path
            else
            {
                bytes = Encoding.ASCII.GetBytes(objectID.filePath);
                md4.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
            }

            bytes = BitConverter.GetBytes(objectID.localIdentifierInFile);
            md4.TransformFinalBlock(bytes, 0, bytes.Length);
            var hash = BitConverter.ToInt64(md4.Hash, 0);
            return hash;
        }

        private static int Compare(ObjectIdentifier x, ObjectIdentifier y)
        {
            if (x.guid != y.guid)
                return x.guid.CompareTo(y.guid);

            // Notes: Only if both guids are invalid, we should check path first
            var empty = new GUID();
            if (x.guid == empty && y.guid == empty)
                return x.filePath.CompareTo(y.filePath);

            if (x.localIdentifierInFile != y.localIdentifierInFile)
                return x.localIdentifierInFile.CompareTo(y.localIdentifierInFile);

            return x.fileType.CompareTo(y.fileType);
        }

        private static int Compare(SerializationInfo x, SerializationInfo y)
        {
            if (x.serializationIndex != y.serializationIndex)
                return x.serializationIndex.CompareTo(y.serializationIndex);

            return Compare(x.serializationObject, y.serializationObject);
        }
    }
}
