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
    [Description("Advanced Packed Content")]
    public class AdvancedAssetBundleAssetGroupProcessor : AssetBundleAssetGroupProcessor
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
        public AddressableAssetSettings.ProfileSettings.ProfileValue bundleLoadProvider;

        /// <summary>
        /// TODO - doc
        /// </summary>
        public enum AppendBuildTargetMode
        {
            UseGlobalSettings,
            AppendTarget,
            DontAppendTarget
        }

        internal override void Initialize(AddressableAssetSettings settings)
        {
            if(buildPath == null)
                buildPath = settings.profileSettings.CreateProfileValue("StreamingAsssetsBuildPath");
            if(loadPrefix == null)
                loadPrefix = settings.profileSettings.CreateProfileValue("StreamingAssetsLoadPrefix");
            if(bundleLoadProvider == null)
                bundleLoadProvider = settings.profileSettings.CreateProfileValue(typeof(RemoteAssetBundleProvider).FullName, true);
        }

        /// <summary>
        /// TODO - doc
        /// </summary>
        public AppendBuildTargetMode appendBuildTargetMode = AppendBuildTargetMode.UseGlobalSettings;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public BundleMode bundleMode = BundleMode.PackTogether;
        internal override string displayName { get { return "Advanced Packed Content"; } }
        internal override void SerializeForHash(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter, Stream stream)
        {
            formatter.Serialize(stream, appendBuildTargetMode);
            formatter.Serialize(stream, bundleMode);
            formatter.Serialize(stream, buildPath);
            formatter.Serialize(stream, loadPrefix);
            formatter.Serialize(stream, bundleLoadProvider);
        }

        protected override string GetBuildPath(AddressableAssetSettings settings)
        {
            bool append = settings.buildSettings.appendBuildTargetToBundlePaths;
            if (appendBuildTargetMode == AppendBuildTargetMode.DontAppendTarget)
                append = false;
            else if (appendBuildTargetMode == AppendBuildTargetMode.AppendTarget)
                append = true;

            var subDir = "";
            if (append)
                subDir = "/" + EditorUserBuildSettings.activeBuildTarget.ToString();

            return buildPath.Evaluate(settings.profileSettings, settings.activeProfile) + subDir;
        }

        protected override string GetBundleLoadPath(AddressableAssetSettings settings, string bundleName)
        {
            bool append = settings.buildSettings.appendBuildTargetToBundlePaths;
            if (appendBuildTargetMode == AppendBuildTargetMode.DontAppendTarget)
                append = false;
            else if (appendBuildTargetMode == AppendBuildTargetMode.AppendTarget)
                append = true;

            var subDir = "/";
            if (append)
                subDir = "/" + EditorUserBuildSettings.activeBuildTarget.ToString() + "/";

            return loadPrefix.Evaluate(settings.profileSettings, settings.activeProfile) + subDir + bundleName;
        }

        protected override string GetBundleLoadProvider(AddressableAssetSettings settings)
        {
            return bundleLoadProvider.Evaluate(settings.profileSettings, settings.activeProfile);
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
            bundleMode = (BundleMode)EditorGUILayout.EnumPopup("Packing Mode", bundleMode);
            appendBuildTargetMode = (AppendBuildTargetMode)EditorGUILayout.EnumPopup(new GUIContent("Append build target","Append build target to packed content paths"), appendBuildTargetMode);
            ProfileSettingsEditor.ValueGUI(settings, "Build Path", buildPath);
            ProfileSettingsEditor.ValueGUI(settings, "Load Prefix", loadPrefix);
            ProfileSettingsEditor.ValueGUI(settings, "Load Method", bundleLoadProvider);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
