using System.Collections.Generic;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.AssetBundle.DataTypes;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.AssetBundle.Shared
{
    public static class BundlePackingStep
    {
        public static int StepCount { get { return 2; } }

        public static BuildPipelineCodes Build(BuildDependencyInfo buildInfo, out BuildWriteInfo writeInfo, bool useCache = false, BuildProgressTracker progressTracker = null)
        {
            writeInfo = new BuildWriteInfo();

            // Strip out sprite source textures if nothing references them directly
            var spriteSourceProcessor = new SpriteSourceProcessor(useCache, progressTracker);
            var exitCode = spriteSourceProcessor.Convert(buildInfo.assetInfo, out buildInfo.assetInfo);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            // Generate the commandSet from the calculated dependency information
            var commandSetProcessor = new BuildWriteProcessor(useCache, progressTracker);
            exitCode = commandSetProcessor.Convert(buildInfo, out writeInfo);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            return exitCode;
        }
    }
}
