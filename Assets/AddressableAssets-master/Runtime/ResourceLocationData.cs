using System;
using System.Collections.Generic;
using UnityEngine;
using ResourceManagement;
using ResourceManagement.ResourceLocations;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    [Serializable]
    public class ResourceLocationData
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public enum LocationType
        {
            String,
            Int,
            Enum,
            Custom // ??? hmmm
        }
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string address;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string guid;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public LocationType type;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string typeName;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string id;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string provider;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string data;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public long labelMask;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string[] dependencies;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ResourceLocationData(string a, string gu, string i, string prov, LocationType tp, long labels, string tn, string[] deps)
        {
            address = a;
            guid = gu;
            id = i;
            provider = prov;
            typeName = tn;
            type = tp;
            dependencies = deps;
            labelMask = labels;
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public IResourceLocation Create()
        {
            switch (type)
            {
                case LocationType.String: return new ResourceLocationBase<string>(address, id, provider);
                case LocationType.Int: return new ResourceLocationBase<int>(int.Parse(address), id, provider);
                    //case LocationType.Enum: return ResourceLocationBase<Enum>(Enum.Parse(typeof(Enum), address) as typeof, id, provider);
            }
            return null;
        }
    }


    /// <summary>
    /// TODO - doc
    /// </summary>
    [Serializable]
    public class ResourceLocationList
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        [SerializeField]
        public List<ResourceLocationData> locations = new List<ResourceLocationData>();
        /// <summary>
        /// TODO - doc
        /// </summary>
        [SerializeField]
        public List<string> labels = new List<string>();
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ResourceLocationList() { }
        /// <summary>
        /// TODO - doc
        /// </summary>
        public ResourceLocationList(IEnumerable<ResourceLocationData> locs) { locations.AddRange(locs); }
        /// <summary>
        /// TODO - doc
        /// </summary>
        public bool IsEmpty { get { return locations.Count == 0 && labels.Count == 0; } }
    }
}
