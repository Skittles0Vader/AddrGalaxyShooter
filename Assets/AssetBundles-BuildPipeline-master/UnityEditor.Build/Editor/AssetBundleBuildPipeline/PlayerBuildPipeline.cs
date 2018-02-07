using System.Diagnostics;
using System.IO;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.Player;

namespace UnityEditor.Build.Player
{
    public static class PlayerBuildPipeline
    {
        public const string kTempPlayerBuildPath = "Temp/PlayerBuildData";

        public static ScriptCompilationSettings GeneratePlayerBuildSettings()
        {
            var settings = new ScriptCompilationSettings();
            settings.target = EditorUserBuildSettings.activeBuildTarget;
            settings.group = BuildPipeline.GetBuildTargetGroup(settings.target);
            settings.options = ScriptCompilationOptions.None;
            return settings;
        }

        public static ScriptCompilationSettings GeneratePlayerBuildSettings(BuildTarget target)
        {
            var settings = new ScriptCompilationSettings();
            settings.target = target;
            settings.group = BuildPipeline.GetBuildTargetGroup(settings.target);
            settings.options = ScriptCompilationOptions.None;
            return settings;
        }

        public static ScriptCompilationSettings GeneratePlayerBuildSettings(BuildTarget target, BuildTargetGroup group)
        {
            var settings = new ScriptCompilationSettings();
            settings.target = target;
            settings.group = group;
            // TODO: Validate target & group
            settings.options = ScriptCompilationOptions.None;
            return settings;
        }

        public static ScriptCompilationSettings GeneratePlayerBuildSettings(BuildTarget target, BuildTargetGroup group, ScriptCompilationOptions options)
        {
            var settings = new ScriptCompilationSettings();
            settings.target = target;
            settings.group = group;
            // TODO: Validate target & group
            settings.options = options;
            return settings;
        }

        public static BuildPipelineCodes BuildPlayerScripts(ScriptCompilationSettings settings, out ScriptCompilationResult result, bool useCache = true)
        {
            var buildTimer = new Stopwatch();
            buildTimer.Start();

            BuildPipelineCodes exitCode;
            using (var progressTracker = new BuildProgressTracker(1))
            {
                using (var buildCleanup = new BuildStateCleanup(false, kTempPlayerBuildPath))
                {
                    var scriptDependency = new ScriptDependency(useCache, progressTracker);
                    exitCode = scriptDependency.Convert(settings, kTempPlayerBuildPath, out result);
                    if (exitCode < BuildPipelineCodes.Success)
                        return exitCode;
                }
            }
            
            buildTimer.Stop();
            if (exitCode >= BuildPipelineCodes.Success)
                BuildLogger.Log("Build Player Scripts successful in: {0:c}", buildTimer.Elapsed);
            else if (exitCode == BuildPipelineCodes.Canceled)
                BuildLogger.LogWarning("Build Player Scripts canceled in: {0:c}", buildTimer.Elapsed);
            else
                BuildLogger.LogError("Build Player Scripts failed in: {0:c}", buildTimer.Elapsed);

            return exitCode;
        }
    }
}
