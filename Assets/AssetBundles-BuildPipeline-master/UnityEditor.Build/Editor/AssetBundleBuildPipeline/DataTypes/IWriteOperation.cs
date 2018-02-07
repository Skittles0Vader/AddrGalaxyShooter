using System.Collections.Generic;
using UnityEditor.Experimental.Build;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle.DataTypes
{
    public interface IWriteOperation
    {
        WriteCommand command { get; }

        List<WriteCommand> CalculateDependencies(List<WriteCommand> allCommands);

        WriteResult Write(string outputFolder, List<WriteCommand> dependencies, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet buildUsage);
    }
}
