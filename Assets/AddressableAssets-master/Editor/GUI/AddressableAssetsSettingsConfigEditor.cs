using UnityEditor;
using UnityEngine;
using System.Linq;

namespace AddressableAssets
{
    [System.Serializable]
    internal class AddressableAssetsSettingsConfigEditor
    {
        private AddressableAssetSettings settingsObject;
        private Texture2D helpIcon;
        
        public AddressableAssetsSettingsConfigEditor(AddressableAssetSettings settingsObject)
        {
            this.settingsObject = settingsObject;
        }

        [SerializeField]
        Vector2 scrollPosition = new Vector2();
        [SerializeField]
        bool expandBuild = false;
        [SerializeField]
        bool expandCatalog = true;
        [SerializeField]
        bool expandSimulation = false;
        [SerializeField]
        bool expandProfile = true;

        public bool OnGUI(Rect pos)
        {
            var bs = settingsObject.buildSettings;

            MyExtensionMethods.DrawOutline(pos, 1f);
            GUILayout.BeginArea(pos);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.MaxWidth(pos.width));

            GUILayout.Space(10);
            bool doBuildNow = false;

            EditorGUILayout.LabelField("Play Mode");

            EditorGUI.indentLevel++;
            bs.resourceProviderMode = (ResourceManagerRuntimeData.ProviderMode)EditorGUILayout.EnumPopup(bs.resourceProviderMode);
            

            switch (bs.resourceProviderMode)
            {
                default:
                case ResourceManagerRuntimeData.ProviderMode.FastMode:
                    EditorGUILayout.HelpBox(new GUIContent("Assets will be loaded directly through the Asset Database.  This mode is for quick iteration and does not try to simulate packed content behavior."), true);
                    break;
                case ResourceManagerRuntimeData.ProviderMode.VirtualMode:

                    EditorGUILayout.HelpBox(new GUIContent("Content is analyzed for packing layout and dependencies, but will not be packed. Assets will load from the Asset Database after going through a virtual bundle layer. This supports utilizing the ResourceManager profiler without having to fully pack contents."), true);

                    //not copied...
                    expandSimulation = EditorGUILayout.Foldout(expandSimulation, "Simulation Settings");
                    if (expandSimulation)
                    {
                        EditorGUI.indentLevel++;
                        bs.localLoadSpeed = (int)(EditorGUILayout.FloatField("Local Load (MB/s)", bs.localLoadSpeed / 1048576f) * 1048576);
                        bs.remoteLoadSpeed = (int)(EditorGUILayout.FloatField("Remote Load (MB/s)", bs.remoteLoadSpeed / 1048576f) * 1048576);
                        EditorGUI.indentLevel--;
                    }

                    break;
                case ResourceManagerRuntimeData.ProviderMode.PackedMode:
                    EditorGUILayout.HelpBox(new GUIContent("Content is fully packed when entering play mode. This mode takes the most amount of time to prepare but provides the most accurate behavior for resource loading."), true);
                    break;
            }

            using (new EditorGUI.DisabledScope(bs.resourceProviderMode == ResourceManagerRuntimeData.ProviderMode.FastMode))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("    Build Data Now    "))
                {
                    doBuildNow = true;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            expandCatalog = EditorGUILayout.Foldout(expandCatalog, "Remote Catalog Settings");
            if (expandCatalog)
            {
                EditorGUI.indentLevel++;
                settingsObject.buildSettings.downloadRemoteCatalog = EditorGUILayout.Toggle(new GUIContent("Download Catalog", "Download remote catalog on game startup"), settingsObject.buildSettings.downloadRemoteCatalog);
                using (new EditorGUI.DisabledScope(!settingsObject.buildSettings.downloadRemoteCatalog))
                {
                    EditorGUI.indentLevel++;
                    ProfileSettingsEditor.ValueGUI(settingsObject, "Remote Location", settingsObject.buildSettings.remoteCatalogLocation);
                    ProfileSettingsEditor.ValueGUI(settingsObject, "Provider", settingsObject.buildSettings.remoteCatalogProvider);
                    ProfileSettingsEditor.ValueGUI(settingsObject, "Build Location", settingsObject.buildSettings.remoteCatalogBuildLocation);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }


            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            expandBuild = EditorGUILayout.Foldout(expandBuild, "Build Settings");
            if (expandBuild)
            {
                EditorGUI.indentLevel++;
                bs.postProfilerEvents = EditorGUILayout.Toggle("Send Profiler Events", bs.postProfilerEvents);
                bs.useCache = EditorGUILayout.Toggle("Use Cache", bs.useCache);
                bs.appendBuildTargetToBundlePaths = EditorGUILayout.Toggle(new GUIContent("Append Build Target", "Append Build Target to packed content paths"), bs.appendBuildTargetToBundlePaths);
                bs.deduplicationMode = (AddressableAssetSettings.BuildSettings.DeduplicationMode)EditorGUILayout.EnumPopup("Deduplication Mode", bs.deduplicationMode);

                using (new EditorGUI.DisabledScope(bs.deduplicationMode < AddressableAssetSettings.BuildSettings.DeduplicationMode.Asset))
                {
                    EditorGUI.indentLevel++;
                    var groupNames = settingsObject.groups.Select(g => g.name).ToList();
                    groupNames.Remove(AddressableAssetSettings.PlayerDataGroupName);
                    //groupNames.Remove(AddressableAssetSettings.AssetReferencesGroupName);
                    bs.sharedBundleTargetGroup = EditorGUILayout.Popup("Target Group", bs.sharedBundleTargetGroup, groupNames.ToArray());
                    using (new EditorGUI.DisabledScope(bs.deduplicationMode != AddressableAssetSettings.BuildSettings.DeduplicationMode.Object))
                    {
                        bs.aggressiveObjectDepude = EditorGUILayout.Toggle("Aggressive", bs.aggressiveObjectDepude);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            

            if (doBuildNow)
            {
                var prev = bs.forceRebuildData;
                bs.forceRebuildData = true;
                BuildScript.PrepareRuntimeData(false, true, true, BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), EditorUserBuildSettings.activeBuildTarget);
                bs.forceRebuildData = prev;
            }

            return false;
        }

        internal void OnEnable(AddressableAssetSettings settings)
        {
        }

        internal void OnDisable()
        {
        }
    }
}
