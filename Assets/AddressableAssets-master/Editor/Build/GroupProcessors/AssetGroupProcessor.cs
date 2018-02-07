using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public class AssetGroupProcessor : ScriptableObject
    {
        internal virtual string displayName { get { return GetType().Name; } }

        internal virtual void Initialize(AddressableAssetSettings settings)
        {
        }

        internal virtual void ProcessGroup(AddressableAssetSettings settings, AddressableAssetSettings.AssetGroup assetGroup, List<BuildInput.Definition> bundleInputDefs, List<ResourceLocationData> locationData)
        {
        }

        internal virtual void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
        }

        internal virtual void CreateResourceLocationData(AddressableAssetSettings settings, ResourceManagerRuntimeData runtimeData, AddressableAssetSettings.AssetGroup assetGroup, string bundleName, List<GUID> assetsInBundle, Dictionary<GUID, List<string>> assetsToBundles, List<ResourceLocationData> locations)
        {
        }

        internal virtual void PostProcessBundles(AddressableAssetSettings aaSettings, AddressableAssetSettings.AssetGroup assetGroup, UnityEditor.Build.AssetBundle.BuildResultInfo buildResult, ResourceManagerRuntimeData runtimeData)
        {
        }

        internal virtual void SerializeForHash(BinaryFormatter formatter, Stream stream)
        {
            
        }
    }
}
