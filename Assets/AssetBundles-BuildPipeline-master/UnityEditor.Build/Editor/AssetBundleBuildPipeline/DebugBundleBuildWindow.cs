using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.Build.AssetBundle;
using UnityEditor.Build.Player;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.Build
{
    public class DebugBundleBuildWindow : EditorWindow
    {
        [Serializable]
        private struct Settings
        {
            public BuildTarget buildTarget;
            public BuildTargetGroup buildGroup;
            public CompressionType compressionType;
            public bool useBuildCache;
            public bool useExperimentalPipeline;
            public string outputPath;
        }

        [SerializeField]
        Settings m_Settings;

        SerializedObject m_SerializedObject;
        SerializedProperty m_TargetProp;
        SerializedProperty m_GroupProp;
        SerializedProperty m_CompressionProp;
        SerializedProperty m_CacheProp;
        SerializedProperty m_ExpProp;
        SerializedProperty m_OutputProp;

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/Build Pipeline/Debug Window")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<DebugBundleBuildWindow>("Debug Build");
            window.m_Settings.buildTarget = EditorUserBuildSettings.activeBuildTarget;
            window.m_Settings.buildGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            window.m_Settings.useExperimentalPipeline = true;

            window.Show();
        }

        private void OnEnable()
        {
            m_SerializedObject = new SerializedObject(this);
            m_TargetProp = m_SerializedObject.FindProperty("m_Settings.buildTarget");
            m_GroupProp = m_SerializedObject.FindProperty("m_Settings.buildGroup");
            m_CompressionProp = m_SerializedObject.FindProperty("m_Settings.compressionType");
            m_CacheProp = m_SerializedObject.FindProperty("m_Settings.useBuildCache");
            m_ExpProp = m_SerializedObject.FindProperty("m_Settings.useExperimentalPipeline");
            m_OutputProp = m_SerializedObject.FindProperty("m_Settings.outputPath");
        }

        private void OnGUI()
        {
            m_SerializedObject.Update();

            EditorGUILayout.PropertyField(m_TargetProp);
            EditorGUILayout.PropertyField(m_GroupProp);
            EditorGUILayout.PropertyField(m_CompressionProp);
            EditorGUILayout.PropertyField(m_CacheProp);
            EditorGUILayout.PropertyField(m_ExpProp);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_OutputProp);
            if (GUILayout.Button("Pick", GUILayout.Width(50)))
                PickOutputFolder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Purge Cache"))
                BuildCache.PurgeCache();
            if (GUILayout.Button("Purge Output"))
                PurgeOutputFolder();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Output"))
                OpenOutputFolder();
            if (GUILayout.Button("Build Bundles"))
                BuildAssetBundles();
            EditorGUILayout.EndHorizontal();

            m_SerializedObject.ApplyModifiedProperties();
        }

        private void OpenOutputFolder()
        {
#if UNITY_EDITOR_WIN
            var folder = new DirectoryInfo(m_Settings.outputPath);
            if (!folder.Exists)
                return;
            try
            {
                Process.Start("explorer.exe", folder.FullName);
            }
            catch
            { }
#elif UNITY_EDITOR_MAC
            var folder = new DirectoryInfo(m_Settings.outputPath);
            if (!folder.Exists)
                return;
            try
            {
                Process.Start("open", string.Format("-R \"{0}\"", folder.FullName));
            }
            catch
            { }
#endif
        }

        private void PickOutputFolder()
        {
            var folder = m_OutputProp.stringValue;
            if (!Directory.Exists(folder))
            {
                folder = "Builds";
                Directory.CreateDirectory(folder);
            }

            // I feed dirty using while(true) =(
            while (true)
            {
                folder = EditorUtility.SaveFolderPanel("Build output location", folder, "");
                if (string.IsNullOrEmpty(folder))
                {
                    GUIUtility.keyboardControl = 0;
                    return;
                }

                if (!BuildPathValidator.ValidOutputFolder(folder, false))
                {
                    if (!EditorUtility.DisplayDialog("Build output location error", string.Format(BuildPathValidator.kPathNotValidError, folder), "Ok", "Cancel"))
                        return;

                    continue;
                }

                var relativeFolder = FileUtil.GetProjectRelativePath(folder);
                m_OutputProp.stringValue = string.IsNullOrEmpty(relativeFolder) ? folder : relativeFolder;
                GUIUtility.keyboardControl = 0;
                return;
            }
        }

        private void PurgeOutputFolder()
        {
            if (!BuildPathValidator.ValidOutputFolder(m_Settings.outputPath, true))
                return;

            if (!EditorUtility.DisplayDialog("Purge Output Folder", "Do you really want to delete your output folder?", "Yes", "No"))
                return;

            if (Directory.Exists(m_Settings.outputPath))
                Directory.Delete(m_Settings.outputPath, true);
        }

        private void BuildAssetBundles()
        {
            if (!BuildPathValidator.ValidOutputFolder(m_Settings.outputPath, true))
            {
                EditorUtility.DisplayDialog("Invalid Output Folder", string.Format(BuildPathValidator.kPathNotValidError, m_Settings.outputPath), "Ok");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (m_Settings.useExperimentalPipeline)
            {
                ExperimentalBuildPipeline();
                return;
            }

            var buildTimer = new Stopwatch();
            buildTimer.Start();

            var exitCode = LegacyBuildPipeline();

            buildTimer.Stop();
            if (exitCode == BuildPipelineCodes.Success)
                BuildLogger.Log("Build Asset Bundles successful in: {0:c}", buildTimer.Elapsed);
            else if (exitCode == BuildPipelineCodes.Canceled)
                BuildLogger.LogWarning("Build Asset Bundles canceled in: {0:c}", buildTimer.Elapsed);
            else
                BuildLogger.LogError("Build Asset Bundles failed in: {0:c}. Error: {1}.", buildTimer.Elapsed, exitCode);
        }

        private BuildPipelineCodes ExperimentalBuildPipeline()
        {
            var playerSettings = PlayerBuildPipeline.GeneratePlayerBuildSettings(m_Settings.buildTarget, m_Settings.buildGroup);
            ScriptCompilationResult scriptResults;
            var errorCode = PlayerBuildPipeline.BuildPlayerScripts(playerSettings, out scriptResults);
            if (errorCode < BuildPipelineCodes.Success)
                return errorCode;

            var bundleSettings = BundleBuildPipeline.GenerateBundleBuildSettings(scriptResults.typeDB, m_Settings.buildTarget, m_Settings.buildGroup);

            BuildCompression compression = BuildCompression.DefaultLZ4;
            if (m_Settings.compressionType == CompressionType.None)
                compression = BuildCompression.DefaultUncompressed;
            else if (m_Settings.compressionType == CompressionType.Lzma)
                compression = BuildCompression.DefaultLZMA;

            BuildResultInfo bundleResult;
            errorCode = BundleBuildPipeline.BuildAssetBundles(BundleBuildInterface.GenerateBuildInput(), bundleSettings, compression, m_Settings.outputPath, out bundleResult, null, m_Settings.useBuildCache);
            return errorCode;
        }

        private BuildPipelineCodes LegacyBuildPipeline()
        {
            var options = BuildAssetBundleOptions.None;
            if (m_Settings.compressionType == CompressionType.None)
                options |= BuildAssetBundleOptions.UncompressedAssetBundle;
            else if (m_Settings.compressionType == CompressionType.Lz4HC || m_Settings.compressionType == CompressionType.Lz4)
                options |= BuildAssetBundleOptions.ChunkBasedCompression;

            if (!m_Settings.useBuildCache)
                options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;

            Directory.CreateDirectory(m_Settings.outputPath);
            var manifest = BuildPipeline.BuildAssetBundles(m_Settings.outputPath, options, m_Settings.buildTarget);
            return manifest != null ? BuildPipelineCodes.Success : BuildPipelineCodes.Error;
        }
    }
}
