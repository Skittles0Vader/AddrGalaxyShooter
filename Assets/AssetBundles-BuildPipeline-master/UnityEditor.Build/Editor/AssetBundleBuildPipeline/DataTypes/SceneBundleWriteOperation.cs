using System.Collections.Generic;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataTypes
{
    public class SceneBundleWriteOperation : SceneDataWriteOperation
    {
        public SceneBundleInfo info { get { return m_Info; } }
        protected SceneBundleInfo m_Info = new SceneBundleInfo();

        public SceneBundleWriteOperation() { }
        public SceneBundleWriteOperation(RawWriteOperation other) : base(other) { }
        public SceneBundleWriteOperation(SceneDataWriteOperation other) : base(other) { }
        public SceneBundleWriteOperation(SceneBundleWriteOperation other) : base(other)
        {
            // Notes: May want to switch to MemberwiseClone, for now those this is fine
            m_Info = other.m_Info;
        }

        public override WriteResult Write(string outputFolder, List<WriteCommand> dependencies, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet buildUsage)
        {
            var referenceMap = new BuildReferenceMap();
            referenceMap.AddMappings(command.internalName, command.serializeObjects.ToArray());
            foreach (var dependency in dependencies)
                referenceMap.AddMappings(dependency.internalName, dependency.serializeObjects.ToArray());
            buildUsage.UnionWith(usageTags);
            return BundleBuildInterface.WriteSceneSerializedFile(outputFolder, scene, processedScene, command, settings, globalUsage, buildUsage, referenceMap, preloadInfo, info);
        }
    }
}
