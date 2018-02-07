using System.ComponentModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor;
using ResourceManagement;
using ResourceManagement.ResourceProviders;
using ResourceManagement.ResourceProviders.Simulation;
using System.IO;

namespace AddressableAssets
{
    [Description("Remote Packed Content")]
    public class RemoteAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public AddressableAssetSettings.ProfileSettings.ProfileValue buildPath;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public AddressableAssetSettings.ProfileSettings.ProfileValue loadPrefix;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public BundleMode bundleMode = BundleMode.PackTogether;

        internal override string displayName { get { return "Remote Packed Content"; } }
        internal override void Initialize(AddressableAssetSettings settings)
        {
            if(buildPath == null)
                buildPath = settings.profileSettings.CreateProfileValue(settings.profileSettings.GetVariableIdFromName("StreamingAsssetsBuildPath"));
            if(loadPrefix == null)
                loadPrefix = settings.profileSettings.CreateProfileValue(settings.profileSettings.GetVariableIdFromName("StreamingAssetsLoadPrefix"));
        }

        internal override void SerializeForHash(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, bundleMode);
            formatter.Serialize(stream, buildPath);
            formatter.Serialize(stream, loadPrefix);
        }

        protected override string GetBuildPath(AddressableAssetSettings settings)
        {
            var subDir = "";
            if (settings.buildSettings.appendBuildTargetToBundlePaths)
                subDir = "/" + EditorUserBuildSettings.activeBuildTarget.ToString();

            return buildPath.Evaluate(settings.profileSettings, settings.activeProfile) + subDir;
        }

        protected override string GetBundleLoadPath(AddressableAssetSettings settings, string bundleName)
        {
            var subDir = "/";
            if (settings.buildSettings.appendBuildTargetToBundlePaths)
                subDir = "/" + EditorUserBuildSettings.activeBuildTarget.ToString() + "/";

            return loadPrefix.Evaluate(settings.profileSettings, settings.activeProfile) + subDir + bundleName;
        }

        protected override string GetBundleLoadProvider(AddressableAssetSettings settings)
        {
            return typeof(RemoteAssetBundleProvider).FullName;
        }

        protected override BundleMode GetBundleMode(AddressableAssetSettings settings)
        {
            return bundleMode;
        }

        [SerializeField]
        Vector2 position = new Vector2();
        internal override void OnDrawGUI(AddressableAssetSettings settings, Rect rect)
        {
            GUILayout.BeginArea(rect);
            position = EditorGUILayout.BeginScrollView(position, false, false, GUILayout.MaxWidth(rect.width));
            EditorGUILayout.LabelField("Assets in this group can either be packed together or separately and will be downloaded from a URL via UnityWebRequest.");

            bundleMode = (BundleMode)EditorGUILayout.EnumPopup("Packing Mode", bundleMode);

            ProfileSettingsEditor.ValueGUI(settings, "Build Path", buildPath);
            ProfileSettingsEditor.ValueGUI(settings, "Load Prefix", loadPrefix);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
