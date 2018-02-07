using System.IO;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.Player;
using UnityEngine;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class ScriptDependency : ADataConverter<ScriptCompilationSettings, string, ScriptCompilationResult>
    {
        public override uint Version { get { return 1; } }

        // TODO: Figure out a way to cache script compiling
        public ScriptDependency(bool useCache, IProgressTracker progressTracker) : base(false, progressTracker) { }

        // TODO: Figure out a way to cache script compiling
        public override bool UseCache
        {
            get { return base.UseCache; }
            set { base.UseCache = false; }
        }

        private Hash128 CalculateInputHash(ScriptCompilationSettings settings)
        {
            if (!UseCache)
                return new Hash128();

            // TODO: Figure out a way to cache script compiling
            return new Hash128();
        }

        public override BuildPipelineCodes Convert(ScriptCompilationSettings settings, string outputFolder, out ScriptCompilationResult output)
        {
            StartProgressBar("Compiling Player Scripts", 1);

            if (!UpdateProgressBar(""))
            {
                output = new ScriptCompilationResult();
                EndProgressBar();
                return BuildPipelineCodes.Canceled;
            }

            Hash128 hash = CalculateInputHash(settings);
            if (UseCache && TryLoadFromCache(hash, outputFolder, out output))
            {
                EndProgressBar();
                return BuildPipelineCodes.SuccessCached;
            }

            output = PlayerBuildInterface.CompilePlayerScripts(settings, outputFolder);
            if (output.assemblies.IsNullOrEmpty() && output.typeDB == null)
                return BuildPipelineCodes.Error;

            if (UseCache && !TrySaveToCache(hash, output, outputFolder))
                BuildLogger.LogWarning("Unable to cache ScriptDependency results.");

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }

        private bool TryLoadFromCache(Hash128 hash, string outputFolder, out ScriptCompilationResult output)
        {
            string rootCachePath;
            string[] artifactPaths;

            if (!BuildCache.TryLoadCachedResultsAndArtifacts(hash, out output, out artifactPaths, out rootCachePath))
                return false;

            Directory.CreateDirectory(outputFolder);

            foreach (var artifact in artifactPaths)
                File.Copy(artifact, artifact.Replace(rootCachePath, outputFolder), true);
            return true;
        }

        private bool TrySaveToCache(Hash128 hash, ScriptCompilationResult output, string outputFolder)
        {
            var artifacts = new string[output.assemblies.Count];
            for (var i = 0; i < output.assemblies.Count; i++)
                artifacts[i] = output.assemblies[i];

            return BuildCache.SaveCachedResultsAndArtifacts(hash, output, artifacts, outputFolder);
        }
    }
}
