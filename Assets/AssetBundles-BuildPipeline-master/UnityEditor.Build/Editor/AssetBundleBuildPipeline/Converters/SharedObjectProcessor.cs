using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class SharedObjectProcessor : ADataConverter<BuildDependencyInfo, BuildSettings, bool, BuildDependencyInfo>
    {
        public override uint Version { get { return 1; } }

        public SharedObjectProcessor(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(BuildDependencyInfo input)
        {
            if (!UseCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(Version, input);
        }

        public override BuildPipelineCodes Convert(BuildDependencyInfo input, BuildSettings settings, bool aggressive, out BuildDependencyInfo output)
        {
            StartProgressBar("Generated shared object bundles", 3);

            Hash128 hash = CalculateInputHash(input);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out output))
            {
                EndProgressBar();
                return BuildPipelineCodes.SuccessCached;
            }

            // Mutating the input
            output = input;

            if (!UpdateProgressBar("Generate lookup of all objects"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            // Generate mapping of each object to the bundles it would be used by
            var objectToBundles = new Dictionary<ObjectIdentifier, HashSet<string>>();
            var objectToAssets = new Dictionary<ObjectIdentifier, HashSet<GUID>>();
            foreach (var asset in input.assetInfo.Values)
            {
                var dependencies = input.assetToBundles[asset.asset];

                if (aggressive && !asset.includedObjects.IsNullOrEmpty())
                {
                    for (int i = 1; i < asset.includedObjects.Count; ++i)
                    {
                        var objectID = asset.includedObjects[i];

                        HashSet<string> bundles;
                        objectToBundles.GetOrAdd(objectID, out bundles);
                        bundles.Add(dependencies[0]);

                        HashSet<GUID> assets;
                        objectToAssets.GetOrAdd(objectID, out assets);
                        assets.Add(asset.asset);
                    }
                }

                foreach (var referenceID in asset.referencedObjects)
                {
                    if (!aggressive && input.assetToBundles.ContainsKey(referenceID.guid))
                        continue;

                    if (referenceID.filePath == BuildWriteProcessor.kUnityDefaultResourcePath)
                        continue;

                    HashSet<string> bundles;
                    objectToBundles.GetOrAdd(referenceID, out bundles);
                    bundles.Add(dependencies[0]);

                    HashSet<GUID> assets;
                    objectToAssets.GetOrAdd(referenceID, out assets);
                    assets.Add(asset.asset);
                }
            }


            if (!UpdateProgressBar("Finding set of reused objects"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            // Generate the set of reused objects
            var hashToObjects = new Dictionary<Hash128, List<ObjectIdentifier>>();
            foreach (var objectPair in objectToBundles)
            {
                if (objectPair.Value.Count <= 1)
                    continue;

                var bundleHash = HashingMethods.CalculateMD5Hash(objectPair.Value.ToArray());

                List<ObjectIdentifier> objectIDs;
                hashToObjects.GetOrAdd(bundleHash, out objectIDs);
                objectIDs.Add(objectPair.Key);
            }


            if (!UpdateProgressBar("Creating shared object bundles"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            // Generate Shared Bundles
            foreach (var hashPair in hashToObjects)
            {
                // Generate Dependency Information for virtual asset
                var assetInfo = new AssetLoadInfo();
                assetInfo.asset = new GUID(hashPair.Key.ToString());
                assetInfo.address = hashPair.Key.ToString();
                assetInfo.includedObjects = hashPair.Value.ToList();
                assetInfo.referencedObjects = new List<ObjectIdentifier>();
                assetInfo.includedObjects.Sort((x, y) => { if (x < y) return -1; if (x > y) return 1; return 0; });

                // Add new AssetLoadInfo for virtual asset
                output.assetInfo.Add(assetInfo.asset, assetInfo);
                var assetBundles = new List<string>();
                assetBundles.Add(assetInfo.address);

                // Add new bundle as dependency[0] for virtual asset
                output.assetToBundles.Add(assetInfo.asset, assetBundles);
                var bundleAssets = new List<GUID>();
                bundleAssets.Add(assetInfo.asset);

                // Add virtual asset to the list of assets for new bundle
                output.bundleToAssets.Add(assetInfo.address, bundleAssets);

                // Add virtual asset to lookup
                output.virtualAssets.Add(assetInfo.asset);

                foreach (var objectID in assetInfo.includedObjects)
                {
                    // Add objects in virtual asset to lookup
                    output.objectToVirtualAsset.Add(objectID, assetInfo.asset);
                    var assets = objectToAssets[objectID];
                    foreach (var asset in assets)
                    {
                        if (!output.assetToBundles.TryGetValue(asset, out assetBundles))
                            continue;

                        if (assetBundles.Contains(assetInfo.address))
                            continue;

                        // Add new bundle as dependency to assets referencing virtual asset objects
                        assetBundles.Add(assetInfo.address);
                    }
                }
            }

            // Generate Shared Bundle Build Dependencies
            foreach (var virtualAsset in output.virtualAssets)
            {
                var assetInfo = output.assetInfo[virtualAsset];
                var dependencies = output.assetToBundles[virtualAsset];

                var references = BundleBuildInterface.GetPlayerDependenciesForObjects(assetInfo.includedObjects.ToArray(), settings.target, settings.typeDB);
                foreach (var reference in references)
                {
                    GUID dependency;
                    List<string> bundles;

                    string depStr = "";

                    // If the reference is to an object in a virtual asset, no major checks, just add it as a dependency
                    if (output.objectToVirtualAsset.TryGetValue(reference, out dependency))
                    {
                        if (dependency == virtualAsset)
                            continue;

                        depStr = dependency.ToString();
                    }
                    // Otherwise if this reference is part of an asset assigned to a bundle, then set the bundle as a dependency to the virtual asset
                    else if (output.assetToBundles.TryGetValue(reference.guid, out bundles))
                    {
                        if (bundles.IsNullOrEmpty())
                            continue;

                        depStr = bundles[0];
                    }

                    if (dependencies.Contains(depStr))
                        continue;

                    dependencies.Add(depStr);
                }
            }

            if (UseCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache SharedObjectProcessor results.");

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }
    }
}
