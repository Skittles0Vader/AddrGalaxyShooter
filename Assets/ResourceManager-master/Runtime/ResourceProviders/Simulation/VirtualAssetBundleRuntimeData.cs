using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace ResourceManagement.ResourceProviders.Simulation
{
    [Serializable]
    public class VirtualAssetBundleRuntimeData
    {
        public List<VirtualAssetBundle> simulatedAssetBundles = new List<VirtualAssetBundle>();
        public string[] sceneGUIDS;
        public int remoteLoadSpeed = 1024 * 100;
        public int localLoadSpeed = 1024 * 1024 * 10;
        public static string PlayerLocation { get { return Path.Combine(Application.streamingAssetsPath, "VirtualAssetBundleData.json").Replace('\\', '/'); } }

        public VirtualAssetBundleRuntimeData() {}
        public VirtualAssetBundleRuntimeData(int localSpeed, int remoteSpeed)
        {
            localLoadSpeed = localSpeed;
            remoteLoadSpeed = remoteSpeed;
        }

        public static VirtualAssetBundleRuntimeData Load()
        {
            try
            {
                if (!File.Exists(PlayerLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(PlayerLocation));
            }
            catch (Exception)
            {
            }
            return null;
        }

#if UNITY_EDITOR
        const string LibraryLocation = "Library/VirtualAssetBundleData.json";
        public static VirtualAssetBundleRuntimeData LoadFromLibrary()
        {
            try
            {
                if (!File.Exists(LibraryLocation))
                    return null;
                return JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(File.ReadAllText(LibraryLocation));
            }
            catch (Exception)
            {
            }
            return null;
        }

        public static void Cleanup()
        {
            if (File.Exists(PlayerLocation))
            {
                File.Delete(PlayerLocation);
                var metaFile = PlayerLocation + ".meta";
                if (File.Exists(metaFile))
                    System.IO.File.Delete(metaFile);
            }
        }

        public void Save()
        {
            var data = JsonUtility.ToJson(this);
            if (!Directory.Exists(Path.GetDirectoryName(PlayerLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(PlayerLocation));
            if (!Directory.Exists(Path.GetDirectoryName(LibraryLocation)))
                Directory.CreateDirectory(Path.GetDirectoryName(LibraryLocation));
            File.WriteAllText(PlayerLocation, data);
            File.WriteAllText(LibraryLocation, data);
        }

#endif
    }
}
