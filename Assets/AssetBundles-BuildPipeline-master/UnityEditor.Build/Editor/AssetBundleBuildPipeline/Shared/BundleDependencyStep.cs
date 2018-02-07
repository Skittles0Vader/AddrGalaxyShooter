using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.Shared
{
    public static class BundleDependencyStep
    {
        public static int StepCount { get { return 2; } }

        public static BuildPipelineCodes Build(BuildInput input, BuildSettings settings, out BuildDependencyInfo buildInfo, bool useCache = false, BuildProgressTracker progressTracker = null)
        {
            buildInfo = null;

            // Rebuild sprite atlas cache for correct dependency calculation & writing
            var spriteCacher = new SpriteAtlasCacher(useCache, progressTracker);
            var exitCode = spriteCacher.Convert(settings.target);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            // Generate dependency information for all assets in BuildInput
            var buildInputDependency = new BuildInputDependency(useCache, progressTracker);
            exitCode = buildInputDependency.Convert(input, settings, out buildInfo);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            return exitCode;
        }
    }
}
