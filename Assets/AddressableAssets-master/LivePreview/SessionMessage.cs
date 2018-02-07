#if LIVE_PREVIEW
using System;

namespace AddressableAssets.LivePreview
{

    [Serializable]
    public struct RuntimeBundleIdentfier
    {
        public string BundleName;
        public uint Version;

        public RuntimeBundleIdentfier(string bundleName, uint version)
        {
            BundleName = bundleName;
            Version = version;
        }
    }

    [Serializable]
    public class DLPResourceManagerMessageLocate
    {
        public enum AddressType
        {
            AssetPath,
            GUID,
            BundleName
        }

        [Serializable]
        public class Query
        {
            public Query(string address, AddressType type)
            {
                Address = address;
                AddressType = type;
            }

            public Query() { }

            public string Address;
            public AddressType AddressType;
        }

        [Serializable]
        public class Result
        {
            public RuntimeBundleIdentfier[] Bundles;

            public Result(RuntimeBundleIdentfier[] bundles)
            {
                Bundles = bundles;
            }

            public Result() { }
        }
    }

    [Serializable]
    public class HotReloadCheckMessage
    {
        [Serializable]
        public class Query
        {
            public Query() { }
            public Query(int index) { Index = index; }

            public int Index;
        }

        [Serializable]
        public class Result
        {
            public string[] ChangedBundleNames;
            public int NewIndex;
        }
    }

    [Serializable]
    public class HotReloadDataMessage
    {
        [Serializable]
        public class Query
        {
            public DLPResourceManagerMessageLocate.Query[] Locators;
        }

        [Serializable]
        public class Result
        {
            public DLPResourceManagerMessageLocate.Result[] Results;
        }
    }

    public class MessageUtil
    {
        public static byte[] Serialize<T>(T e) where T : new()
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter =
                new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(ms, e);
            ms.Flush();
            return ms.ToArray();
        }

        public static object Deserialize(byte[] d)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(d);
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            object ret = formatter.Deserialize(ms);
            ms.Close();
            return ret;
        }
    }
}
#endif