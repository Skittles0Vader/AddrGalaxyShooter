using System.Collections.Generic;
using UnityEngine;
using ResourceManagement;
using System;
using System.IO;
using ResourceManagement.ResourceProviders.Experimental;
using ResourceManagement.ResourceProviders;
using ResourceManagement.ResourceLocators;
using ResourceManagement.Util;
using ResourceManagement.AsyncOperations;
using UnityEngine.Networking;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public class ResourceManagerRuntimeData
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public enum ProviderMode
        {
            FastMode,
            VirtualMode,
            PackedMode
        }
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string settingsHash;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ProviderMode resourceProviderMode = ProviderMode.VirtualMode;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ResourceLocationList contentCatalog = new ResourceLocationList();
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string downloadCatalogLocation = "";
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string downloadCatalogProvider = "";
        /// <summary>
        /// TODO - doc
        /// </summary>
        public bool usePooledInstanceProvider = true;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public bool profileEvents = true;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public bool datalessPlayer = false;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string sessionId;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public static string PlayerLocation { get { return Path.Combine(Application.streamingAssetsPath, "ResourceManagerRuntimeData.json").Replace('\\', '/'); } }

        /// <summary>
        /// TODO - doc
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void InitializeResourceManager()
        {
            EditorDiagnostics.EventCollector.profileEvents = true;
            ResourceManager.m_postEvents = true;

            ResourceManager.resourceLocators.Add(new ResourceLocationLocator());
            ResourceManager.resourceProviders.Add(new JSONAssetProvider());
            var runtimeDataLocation = new ResourceLocationBase<string>("runtimedata", "file://" + PlayerLocation, typeof(JSONAssetProvider).FullName);
            var initOperation = ResourceManager.LoadAsync<ResourceManagerRuntimeData, IResourceLocation>(runtimeDataLocation);
            ResourceManager.QueueInitializationOperation(initOperation);
            initOperation.completed += (op) =>
            {
                if(op.result != null)
                    op.result.Init();
            };
        }

        internal void Init()
        {
            if (usePooledInstanceProvider)
                ResourceManager.instanceProvider = new PooledInstanceProvider("PooledInstanceProvider", 2);
            else
                ResourceManager.instanceProvider = new InstanceProvider();

            ResourceManager.sceneProvider = new SceneProvider();

            EditorDiagnostics.EventCollector.profileEvents = profileEvents;
            ResourceManager.m_postEvents = profileEvents;

            AddResourceProviders();

            ResourceManager.resourceLocators.Add(new ResourceLocationLocator());
			      ResourceManager.resourceLocators.Add(new AssetReferenceLocator((assetRef) => ResourceManager.GetResourceLocation(assetRef.assetGUID)));


            if (!string.IsNullOrEmpty(downloadCatalogLocation))
            {
                var catalogLocation = new ResourceLocationBase<string>(downloadCatalogLocation, downloadCatalogLocation, downloadCatalogProvider);
                var loadOp = ResourceManager.LoadAsync<ResourceLocationList, IResourceLocation>(catalogLocation);
                ResourceManager.QueueInitializationOperation(loadOp);
                loadOp.completed += (op) =>
                    {
                        if (op.result != null)
                            AddContentCatalogs(op.result);
                        else
                            AddContentCatalogs(contentCatalog);
                    };
            }
            else
            {
                AddContentCatalogs(contentCatalog);
            }
        }

        private void AddResourceProviders()
        {
            switch (resourceProviderMode)
            {
                case ProviderMode.FastMode:
                    ResourceManager.resourceProviders.Insert(0, new AssetDatabaseProvider());
                    break;
                case ProviderMode.VirtualMode:
                    ResourceManagement.ResourceProviders.Simulation.VirtualAssetBundleManager.AddProviders();
                    break;
                case ProviderMode.PackedMode:
                {
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new BundledAssetProvider()));
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new LocalAssetBundleProvider()));
                    ResourceManager.resourceProviders.Insert(0, new CachedProvider(new RemoteAssetBundleProvider()));
                }
                break;
            }
        }

        private static void AddContentCatalogs(ResourceLocationList locations)
        {
            var locMap = new Dictionary<string, IResourceLocation>();
            var dataMap = new Dictionary<string, ResourceLocationData>();
            //create and collect locations
            for (int i = 0; i < locations.locations.Count; i++)
            {
                var rlData = locations.locations[i];
                var loc = rlData.Create();
                locMap.Add(rlData.address, loc);
                dataMap.Add(rlData.address, rlData);
            }

            //fix up dependencies between them
            foreach (var kvp in locMap)
            {
                var deps = kvp.Value.dependencies;
                var data = dataMap[kvp.Key];
                if (data.dependencies != null)
                {
                    foreach (var d in data.dependencies)
                        kvp.Value.dependencies.Add(locMap[d]);
                }
            }

            //put them in the correct lookup table
            var ccString = new ResourceLocationMap<string>();
            var ccInt = new ResourceLocationMap<int>();
            var ccEnum = new ResourceLocationMap<Enum>();

            foreach (KeyValuePair<string, IResourceLocation> kvp in locMap)
            {
                IResourceLocation loc = kvp.Value;
                ResourceLocationData rlData = dataMap[kvp.Key];
                switch (rlData.type)
                {
                    case ResourceLocationData.LocationType.String: AddToCatalog(locations.labels, ccString, loc, rlData.labelMask); break;
                    case ResourceLocationData.LocationType.Int: AddToCatalog(locations.labels, ccInt, loc, rlData.labelMask); break;
                    case ResourceLocationData.LocationType.Enum: AddToCatalog(locations.labels, ccEnum, loc, rlData.labelMask); break;
                }
                if (!string.IsNullOrEmpty(rlData.guid) && !ccString.m_addressMap.ContainsKey(rlData.guid))
                    ccString.m_addressMap.Add(rlData.guid, loc as IResourceLocation<string>);
            }
            if (ccString.m_addressMap.Count > 0)
                ResourceManager.resourceLocators.Insert(0, ccString);
            if (ccInt.m_addressMap.Count > 0)
                ResourceManager.resourceLocators.Insert(0, ccInt);
            if (ccEnum.m_addressMap.Count > 0)
                ResourceManager.resourceLocators.Insert(0, ccEnum);
        }

        private static void AddToCatalog<T>(List<string> labels, ResourceLocationMap<T> locations, IResourceLocation loc, long labelMask)
        {
            var locT = loc as IResourceLocation<T>;
            locations.m_addressMap.Add(locT.key, locT);
            for (int t = 0; t < labels.Count; t++)
            {
                if ((labelMask & (1 << t)) != 0)
                {
                    IList<T> results = null;
                    if (!locations.m_labeledGroups.TryGetValue(labels[t], out results))
                        locations.m_labeledGroups.Add(labels[t], results = new List<T>());
                    results.Add(locT.key);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ResourceManagerRuntimeData(ProviderMode mode)
        {
            resourceProviderMode = mode;
        }

        static string LibraryLocation(string mode)
        {
            return "Library/ResourceManagerRuntimeData_" + mode + ".json";
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public static ResourceManagerRuntimeData LoadFromLibrary(string mode)
        {
            try
            {
                if (!System.IO.File.Exists(LibraryLocation(mode)))
                    return null;
                return JsonUtility.FromJson<ResourceManagerRuntimeData>(System.IO.File.ReadAllText(LibraryLocation(mode)));
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public static void Cleanup()
        {
            if (File.Exists(PlayerLocation))
            {
                System.IO.File.Delete(PlayerLocation);
                var metaFile = PlayerLocation + ".meta";
                if (File.Exists(metaFile))
                    System.IO.File.Delete(metaFile);
            }
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public void Save(string mode)
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(PlayerLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(PlayerLocation));
            if (!Directory.Exists(Path.GetDirectoryName(LibraryLocation(mode))))
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryLocation(mode)));
            File.WriteAllText(PlayerLocation, data);
            File.WriteAllText(LibraryLocation(mode), data);
        }

#endif
    }
}
