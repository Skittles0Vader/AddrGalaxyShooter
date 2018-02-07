using System;
using System.Collections.Generic;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

using AssetInfoMap = System.Collections.Generic.Dictionary<UnityEditor.GUID, UnityEditor.Experimental.Build.AssetBundle.AssetLoadInfo>;
using SpriteRefMap = System.Collections.Generic.Dictionary<UnityEditor.Experimental.Build.AssetBundle.ObjectIdentifier, int>;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class SpriteSourceProcessor : ADataConverter<AssetInfoMap, AssetInfoMap>
    {
        public override uint Version { get { return 1; } }

        public SpriteSourceProcessor(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        private Hash128 CalculateInputHash(AssetInfoMap assetLoadInfo, SpriteRefMap spriteRefCount)
        {
            if (!UseCache)
                return new Hash128();

            return HashingMethods.CalculateMD5Hash(Version, assetLoadInfo, spriteRefCount);
        }

        public override BuildPipelineCodes Convert(AssetInfoMap assetLoadInfo, out AssetInfoMap output)
        {
            StartProgressBar("Stripping unused sprite source textures", 3);

            if (!UpdateProgressBar("Finding sprite source textures"))
            {
                output = null;
                return BuildPipelineCodes.Canceled;
            }
            var spriteRefCount = new Dictionary<ObjectIdentifier, int>();
            foreach (var assetInfo in assetLoadInfo)
            {
                var path = AssetDatabase.GUIDToAssetPath(assetInfo.Value.asset.ToString());
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType == TextureImporterType.Sprite && !string.IsNullOrEmpty(importer.spritePackingTag))
                    spriteRefCount[assetInfo.Value.includedObjects[0]] = 0;
            }

            Hash128 hash = CalculateInputHash(assetLoadInfo, spriteRefCount);
            if (UseCache && BuildCache.TryLoadCachedResults(hash, out output))
            {
                EndProgressBar();
                return BuildPipelineCodes.SuccessCached;
            }

            // Mutating the input, this is the only converter that does this
            output = assetLoadInfo;

            if (!UpdateProgressBar("Finding sprite source textures usage"))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }
            foreach (var assetInfo in output)
            {
                foreach (var reference in assetInfo.Value.referencedObjects)
                {
                    int refCount = 0;
                    if (!spriteRefCount.TryGetValue(reference, out refCount))
                        continue;

                    // Note: Because pass by value
                    spriteRefCount[reference] = ++refCount;
                }
            }

            if (!UpdateProgressBar("Removing unused sprite source textures."))
            {
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }
            foreach (var source in spriteRefCount)
            {
                if (source.Value > 0)
                    continue;

                var assetInfo = output[source.Key.guid];
                var includedObjects = assetInfo.includedObjects;
                includedObjects.RemoveAt(0);
            }

            if (UseCache && !BuildCache.SaveCachedResults(hash, output))
                BuildLogger.LogWarning("Unable to cache SpriteSourceProcessor results.");

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }
    }
}
