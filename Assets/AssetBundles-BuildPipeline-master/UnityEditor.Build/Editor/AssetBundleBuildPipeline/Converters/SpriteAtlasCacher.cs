using UnityEditor.Build.Utilities;
using UnityEditor.Sprites;

namespace UnityEditor.Build.AssetBundle.DataConverters
{
    public class SpriteAtlasCacher : ADataConverter
    {
        public override uint Version { get { return 1; } }

        public SpriteAtlasCacher(bool useCache, IProgressTracker progressTracker) : base(useCache, progressTracker) { }

        public BuildPipelineCodes Convert(BuildTarget target)
        {
            StartProgressBar("Rebuilding Atlas Cache", 1);

            // Rebuild sprite atlas cache for correct dependency calculation & writing
            // TODO: need RebuildAtlasCacheIfNeeded to return boolean on if it completed successfully or not
            Packer.RebuildAtlasCacheIfNeeded(target, true, Packer.Execution.Normal);

            if (!EndProgressBar())
                return BuildPipelineCodes.Canceled;
            return BuildPipelineCodes.Success;
        }
    }
}
