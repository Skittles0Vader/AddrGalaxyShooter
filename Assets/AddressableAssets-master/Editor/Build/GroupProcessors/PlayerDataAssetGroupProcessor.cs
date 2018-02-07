using System.Collections.Generic;
using ResourceManagement.ResourceProviders;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.Build.AssetBundle;
using System.ComponentModel;

namespace AddressableAssets
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    [Description("")]
    public class PlayerDataAssetGroupProcessor : AssetGroupProcessor
    {
        internal override string displayName { get { return "Player Data"; } }

        internal override void ProcessGroup(AddressableAssetSettings settings, AddressableAssetSettings.AssetGroup assetGroup, List<BuildInput.Definition> bundleInputDefs, List<ResourceLocationData> locationData)
        {
            foreach (var e in assetGroup.entries)
            {
                var assets = new List<AddressableAssetSettings.AssetGroup.AssetEntry>();
                e.GatherAllAssets(assets, settings);
                foreach (var s in assets)
                {
                    var path = s.assetPath;
                    if (path.EndsWith(".unity"))
                    {
                        locationData.Add(new ResourceLocationData(s.address, s.guid, System.IO.Path.GetFileNameWithoutExtension(path), typeof(SceneProvider).FullName, ResourceLocationData.LocationType.String, 0, typeof(UnityEngine.SceneManagement.Scene).FullName, null));
                        var indexInSceneList = IndexOfSceneInEditorBuildSettings(new GUID(s.guid));
                        if (indexInSceneList >= 0)
                            locationData.Add(new ResourceLocationData(indexInSceneList.ToString(), s.guid, System.IO.Path.GetFileNameWithoutExtension(path), typeof(SceneProvider).FullName, ResourceLocationData.LocationType.Int, 0, typeof(UnityEngine.SceneManagement.Scene).FullName, null));
                    }
                    else
                        locationData.Add(new ResourceLocationData(s.address, s.guid, GetLoadPath(path), typeof(SceneProvider).FullName, ResourceLocationData.LocationType.String, 0, typeof(UnityEngine.SceneManagement.Scene).FullName, null));
                }
            }
        }

        static int IndexOfSceneInEditorBuildSettings(GUID guid)
        {
            int index = 0;
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].enabled)
                {
                    if (EditorBuildSettings.scenes[i].guid == guid)
                        return index;
                    index++;
                }
            }
            return -1;
        }

        static string GetLoadPath(string path)
        {
            int ri = path.ToLower().LastIndexOf("resources/");
            if (ri == 0 || (ri > 0 && path[ri - 1] == '/'))
                path = path.Substring(ri + "resources/".Length);
            int i = path.LastIndexOf('.');
            if (i > 0)
                path = path.Substring(0, i);
            return path;
        }

        [SerializeField]
        Vector2 position = new Vector2();
        internal override void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
            GUILayout.BeginArea(rect);
            position = EditorGUILayout.BeginScrollView(position, false, false, GUILayout.MaxWidth(rect.width));

            EditorStyles.label.wordWrap = true;
            EditorGUILayout.LabelField("Player Data Processor");
            EditorGUILayout.LabelField("This processor handles proper building of all assets stored in Resources and the scenes that are included in the build in BuildSettings window. All data built here will be included in \"Player Data\" in the build of the game.");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
