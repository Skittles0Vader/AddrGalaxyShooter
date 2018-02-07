using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Build.AssetBundle.DataTypes;
using UnityEditor.Build.AssetBundle.Shared;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Build.AssetBundle
{
    public static class BundleBuildPipeline
    {
        public const string kTempBundleBuildPath = "Temp/BundleBuildData";

        public const string kDefaultOutputPath = "AssetBundles";

        // TODO: Replace with calls to UnityEditor.Build.BuildPipelineInterfaces once i make it more generic & public
        public static Func<BuildDependencyInfo, object, BuildPipelineCodes> PostBuildDependency;

        public static Func<BuildDependencyInfo, BuildWriteInfo, object, BuildPipelineCodes> PostBuildPacking;

        public static Func<BuildDependencyInfo, BuildWriteInfo, BuildResultInfo, object, BuildPipelineCodes> PostBuildWriting;

        public static BuildSettings GenerateBundleBuildSettings(TypeDB typeDB)
        {
            var settings = new BuildSettings();
            settings.target = EditorUserBuildSettings.activeBuildTarget;
            settings.group = BuildPipeline.GetBuildTargetGroup(settings.target);
            settings.typeDB = typeDB;
            return settings;
        }

        public static BuildSettings GenerateBundleBuildSettings(TypeDB typeDB, BuildTarget target)
        {
            var settings = new BuildSettings();
            settings.target = target;
            settings.group = BuildPipeline.GetBuildTargetGroup(settings.target);
            settings.typeDB = typeDB;
            return settings;
        }

        public static BuildSettings GenerateBundleBuildSettings(TypeDB typeDB, BuildTarget target, BuildTargetGroup group)
        {
            var settings = new BuildSettings();
            settings.target = target;
            settings.group = group;
            settings.typeDB = typeDB;
            // TODO: Validate target & group
            return settings;
        }

        public static BuildPipelineCodes BuildAssetBundles(BuildInput input, BuildSettings settings, BuildCompression compression, string outputFolder, out BuildResultInfo result, object callbackUserData = null, bool useCache = true)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            if (ProjectValidator.HasDirtyScenes())
            {
                result = new BuildResultInfo();
                buildTimer.Stop();
                BuildLogger.LogError("Build Asset Bundles failed in: {0:c}. Error: {1}.", buildTimer.Elapsed, BuildPipelineCodes.UnsavedChanges);
                return BuildPipelineCodes.UnsavedChanges;
            }

            var exitCode = BuildPipelineCodes.Success;
            result = new BuildResultInfo();

            AssetDatabase.SaveAssets();

            // TODO: Until new AssetDatabaseV2 is online, we need to switch platforms
            EditorUserBuildSettings.SwitchActiveBuildTarget(settings.group, settings.target);

            var stepCount = BundleDependencyStep.StepCount + BundlePackingStep.StepCount + BundleWritingStep.StepCount;
            using (var progressTracker = new BuildProgressTracker(stepCount))
            {
                using (var buildCleanup = new BuildStateCleanup(true, kTempBundleBuildPath))
                {
                    BuildDependencyInfo buildInfo;
                    exitCode = BundleDependencyStep.Build(input, settings, out buildInfo, useCache, progressTracker);
                    if (exitCode < BuildPipelineCodes.Success)
                        return exitCode;

                    if (PostBuildDependency != null)
                    {
                        exitCode = PostBuildDependency.Invoke(buildInfo, callbackUserData);
                        if (exitCode < BuildPipelineCodes.Success)
                            return exitCode;
                    }

                    BuildWriteInfo writeInfo;
                    exitCode = BundlePackingStep.Build(buildInfo, out writeInfo, useCache, progressTracker);
                    if (exitCode < BuildPipelineCodes.Success)
                        return exitCode;

                    if (PostBuildPacking != null)
                    {
                        exitCode = PostBuildPacking.Invoke(buildInfo, writeInfo, callbackUserData);
                        if (exitCode < BuildPipelineCodes.Success)
                            return exitCode;
                    }

                    exitCode = BundleWritingStep.Build(settings, compression, outputFolder, buildInfo, writeInfo, out result, useCache, progressTracker);
                    if (exitCode < BuildPipelineCodes.Success)
                        return exitCode;

                    if (PostBuildWriting != null)
                    {
                        exitCode = PostBuildWriting.Invoke(buildInfo, writeInfo, result, callbackUserData);
                        if (exitCode < BuildPipelineCodes.Success)
                            return exitCode;
                    }
                }
            }

            buildTimer.Stop();
            if (exitCode >= BuildPipelineCodes.Success)
                BuildLogger.Log("Build Asset Bundles successful in: {0:c}", buildTimer.Elapsed);
            else if (exitCode == BuildPipelineCodes.Canceled)
                BuildLogger.LogWarning("Build Asset Bundles canceled in: {0:c}", buildTimer.Elapsed);
            else
                BuildLogger.LogError("Build Asset Bundles failed in: {0:c}. Error: {1}.", buildTimer.Elapsed, exitCode);

            return exitCode;
        }
    }
}