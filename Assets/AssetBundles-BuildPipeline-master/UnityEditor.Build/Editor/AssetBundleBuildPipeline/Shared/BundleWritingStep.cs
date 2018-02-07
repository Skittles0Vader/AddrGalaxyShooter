using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.Shared
{
    public static class BundleWritingStep
    {
        public static int StepCount { get { return 2; } }

        public static BuildPipelineCodes Build(BuildSettings settings, BuildCompression compression, string outputFolder, BuildDependencyInfo buildInfo, BuildWriteInfo writeInfo, out BuildResultInfo result, bool useCache = false, BuildProgressTracker progressTracker = null)
        {
            result = new BuildResultInfo();

            // Write out resource files
            var commandSetWriter = new BuildWriteWriter(useCache, progressTracker);
            var exitCode = commandSetWriter.Convert(writeInfo, settings, buildInfo.buildGlobalUsage, out result);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            // Archive and compress resource files
            var resourceArchiver = new ResourceFileArchiver(useCache, progressTracker);
            exitCode = resourceArchiver.Convert(result, compression, outputFolder, out result.bundleCRCs);
            if (exitCode < BuildPipelineCodes.Success)
                return exitCode;

            // Generate Unity5 compatible manifest files
            //string[] manifestfiles;
            //var manifestWriter = new Unity5ManifestWriter(useCache, true);
            //if (!manifestWriter.Convert(commandSet, output, crc, outputFolder, out manifestfiles))
            //    return false;

            return exitCode;
        }
    }
}
