using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;


// NOTE: partially qualified material loses its reference to the default shader
// Do we need to add references in the commandSet or bundle dependencies for the PPTR to resolve?
#if false //needs to be updated to lates AB-BP API
namespace AddressableAssets.LivePreview
{
    public enum AssetState
    {
        Unqualified = 0,
        FullyQualified = 1,
        PartiallyQualified = 2
    }

    public class LiveAsset
    {
        public AssetState state = AssetState.Unqualified;
        public AssetLoadInfo loadInfo = new AssetLoadInfo();
        public HashSet<GUID> references = new HashSet<GUID>();
    }

    public struct BundleIdentifier
    {
        public string BundleName;
        public uint VersionHash;
        public override string ToString()
        {
            return BundleName + "_" + VersionHash;
        }
    }

    public class LivePreviewBuilder
    {
        private string mCachePath;
        private TypeDB mTypeDB;
        private BuildTarget mBuildTarget;
        private BuildTargetGroup mBuildTargetGroup;
        private List<GUID> mStaleGUIDList;

        public LivePreviewBuilder(BuildTarget target, BuildTargetGroup group, TypeDB typeDB, string cachePath)
        {
            mBuildTarget = target;
            mBuildTargetGroup = group;
            mStaleGUIDList = new List<GUID>();
            mTypeDB = typeDB;
            mCachePath = cachePath;
            Directory.CreateDirectory(cachePath);
            //mTempPath = FileUtil.GetUniqueTempPathInProject();
            //Directory.CreateDirectory(mTempPath);
        }

        Dictionary<GUID, LiveAsset> m_Assets = new Dictionary<GUID, LiveAsset>();

        Dictionary<GUID, ResourceFile[]> m_sceneGI = new Dictionary<GUID, ResourceFile[]>();

        BuildUsageTagGlobal m_globalUsageTags;

        private AssetDependency m_AssetDependency = new AssetDependency(true, null);
        private SceneDependency m_SceneDependency = new SceneDependency(true, null);
        private CommandSetWriter m_CommandWriter = new CommandSetWriter(true, null);
        private ResourceFileArchiver m_Archiver = new ResourceFileArchiver(true, null);

        public AssetState GetStateForAsset(GUID assetGUID)
        {
            LiveAsset asset;
            if (!m_Assets.TryGetValue(assetGUID, out asset))
                return AssetState.Unqualified;
            return asset.state;
        }

        private static void AddReferencesToSet(List<ObjectIdentifier> references, HashSet<GUID> referenceSet)
        {
            foreach (var objectID in references)
            {
                if (objectID.filePath == CommandSetProcessor.kUnityDefaultResourcePath)
                    continue;
                referenceSet.Add(objectID.guid);
            }
        }

        public void FullyCalculateAsset(GUID asset, BuildSettings settings)
        {
            var assetInfo = new LiveAsset();
            assetInfo.state = AssetState.FullyQualified;

            if (SceneDependency.ValidScene(asset))
            {
                SceneLoadInfo sceneInfo;
                if (m_SceneDependency.Convert(asset, settings, out sceneInfo) < 0)
                    throw new Exception();

                assetInfo.loadInfo.asset = asset;
                assetInfo.loadInfo.address = AssetDatabase.GUIDToAssetPath(asset.ToString());
                assetInfo.loadInfo.processedScene = sceneInfo.processedScene;
                assetInfo.loadInfo.includedObjects = new List<ObjectIdentifier>();
                assetInfo.loadInfo.referencedObjects = new List<ObjectIdentifier>(sceneInfo.referencedObjects);
                AddReferencesToSet(assetInfo.loadInfo.referencedObjects, assetInfo.references);

                m_sceneGI.Add(asset, sceneInfo.resourceFiles.ToArray());
                m_globalUsageTags |= sceneInfo.globalUsage;

                m_Assets[asset] = assetInfo;
            }
            else if (AssetDependency.ValidAsset(asset))
            {
                if (m_AssetDependency.Convert(asset, settings, out assetInfo.loadInfo) < 0)
                    throw new Exception();

                assetInfo.loadInfo.address = "main";// AssetDatabase.GUIDToAssetPath(asset.ToString());
                AddReferencesToSet(assetInfo.loadInfo.referencedObjects, assetInfo.references);

                m_Assets[asset] = assetInfo;
            }
            else
            {
                throw new Exception();
            }

            PartiallyCalculateReferences(assetInfo.loadInfo.referencedObjects);
        }

        public void PartiallyCalculateReferences(List<ObjectIdentifier> objectIDs)
        {
            var touchedAssets = new HashSet<LiveAsset>();

            foreach (var objectID in objectIDs)
            {
                if (GetStateForAsset(objectID.guid) == AssetState.FullyQualified)
                    continue;

                if (objectID.filePath == CommandSetProcessor.kUnityDefaultResourcePath)
                    continue;

                LiveAsset assetInfo;
                if (!m_Assets.TryGetValue(objectID.guid, out assetInfo))
                {
                    assetInfo = new LiveAsset();
                    assetInfo.loadInfo.asset = objectID.guid;
                    assetInfo.loadInfo.address = AssetDatabase.GUIDToAssetPath(objectID.guid.ToString()); // why do we need an addressable asset for partial bundles?
                    assetInfo.loadInfo.includedObjects = new List<ObjectIdentifier>();
                    assetInfo.loadInfo.referencedObjects = new List<ObjectIdentifier>();
                    assetInfo.state = AssetState.PartiallyQualified;
                    m_Assets[objectID.guid] = assetInfo;
                }

                if (assetInfo.loadInfo.includedObjects.Contains(objectID))
                    continue;

                // TODO: This is ugly
                var includedObjects = new List<ObjectIdentifier>(assetInfo.loadInfo.includedObjects);
                includedObjects.Add(objectID);
                assetInfo.loadInfo.includedObjects = includedObjects;
                touchedAssets.Add(assetInfo);
            }

            foreach (var assetInfo in touchedAssets)
            {
                assetInfo.loadInfo.referencedObjects = new List<ObjectIdentifier>(BundleBuildInterface.GetPlayerDependenciesForObjects(assetInfo.loadInfo.includedObjects.ToArray(), mBuildTarget, mTypeDB));
                AddReferencesToSet(assetInfo.loadInfo.referencedObjects, assetInfo.references);
            }
        }

        private BundleIdentifier[] GetBundlesForGUID(GUID guid, BuildSettings settings, BuildCompression compression, string outputFolder, bool fullyQualified)
        {
            if (GetStateForAsset(guid) != AssetState.FullyQualified && fullyQualified)
                FullyCalculateAsset(guid, settings);

            LiveAsset asset;
            if (!m_Assets.TryGetValue(guid, out asset))
                throw new Exception();

            var commands = new List<WriteCommand>();
            commands.Add(CreateCommandForAssetLoadInfo(asset));
            foreach (var reference in asset.references)
            {
                LiveAsset refAsset;
                if (!m_Assets.TryGetValue(reference, out refAsset))
                    continue;
                commands.Add(CreateCommandForAssetLoadInfo(refAsset));
            }

            var commandSet = new BuildCommandSet();
            commandSet.commands = commands;

            List<WriteResult> results;
            if (m_CommandWriter.Convert(commandSet, settings, out results) < 0)
                throw new Exception();

            Dictionary<string, uint> bundleCRCs;
            if (m_Archiver.Convert(results, m_sceneGI, compression, outputFolder, out bundleCRCs) < 0)
                throw new Exception();

            var files = new List<BundleIdentifier>();
            foreach (var result in bundleCRCs)
            {
                string bundleName = result.Key.Substring(outputFolder.Length + 1);
                var newName = string.Format("{0}/{1}_{2}", outputFolder, bundleName, result.Value);
                if (File.Exists(newName))
                    File.Delete(result.Key);
                else
                    File.Move(string.Format("{0}/{1}", outputFolder, bundleName), newName);
                BundleIdentifier id = new BundleIdentifier();
                id.BundleName = bundleName;
                id.VersionHash = result.Value;
                files.Add(id);
            }
            return files.ToArray();
        }

        public BundleIdentifier[] GetBundlesForGUID(GUID guid)
        {
            BuildSettings settings = new BuildSettings();
            settings.group = mBuildTargetGroup;
            settings.target = mBuildTarget;
            settings.typeDB = mTypeDB;
            return GetBundlesForGUID(guid, settings, BuildCompression.DefaultUncompressed, mCachePath, true);
        }

        public WriteCommand CreateCommandForAssetLoadInfo(LiveAsset assetInfo)
        {
            var command = new WriteCommand();
            command.assetBundleName = assetInfo.loadInfo.asset.ToString();
            command.explicitAssets = new List<AssetLoadInfo>() { assetInfo.loadInfo };
            command.assetBundleDependencies = assetInfo.references.ToList().ConvertAll(x => x.ToString());

            var assetBundleObjects = new List<SerializationInfo>();
            foreach (var includedObject in assetInfo.loadInfo.includedObjects)
            {
                assetBundleObjects.Add(new SerializationInfo
                {
                    serializationObject = includedObject,
                    serializationIndex = CommandSetProcessor.SerializationIndexFromObjectIdentifier(includedObject)
                });
            }

            command.assetBundleObjects = assetBundleObjects;
            command.globalUsage = m_globalUsageTags;
            command.sceneBundle = SceneDependency.ValidScene(assetInfo.loadInfo.asset);
            return command;
        }

        public BundleIdentifier[] GetBundlesForBundleName(string bundleName)
        {
            BuildSettings settings = new BuildSettings();
            settings.group = mBuildTargetGroup;
            settings.target = mBuildTarget;
            settings.typeDB = mTypeDB;
            GUID guid = new GUID(bundleName);
            if (GetStateForAsset(guid) == AssetState.Unqualified)
                throw new Exception(string.Format("GetBundlesForBundleName unknown bundle{0}", bundleName));
            return GetBundlesForGUID(guid, settings, BuildCompression.DefaultUncompressed, mCachePath, false);
        }

        public void NotifyAssetModified(GUID guid)
        {
            if (m_Assets.ContainsKey(guid))
                mStaleGUIDList.Add(guid);
        }

        public string GetCacheRelativeBundlePath(BundleIdentifier identifier)
        {
            return identifier.ToString();
        }

        public string GetAbsoluteBundlePath(BundleIdentifier identifier)
        {
            return Path.Combine(mCachePath, GetCacheRelativeBundlePath(identifier));
        }

        public string[] GetStaleBundles(int curIndex, out int newIndex)
        {
            HashSet<GUID> guids = new HashSet<GUID>(mStaleGUIDList.AsEnumerable().Skip(curIndex));
            newIndex = mStaleGUIDList.Count;
            return Array.ConvertAll(guids.ToArray(), id => id.ToString());
        }

        public int GetChangedIndex()
        {
            return mStaleGUIDList.Count;
        }
    }
}

#endif