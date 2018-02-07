using System;
using System.Collections.Generic;
using UnityEditor.Build.AssetBundle.DataTypes;

namespace UnityEditor.Build
{
    [Serializable]
    public class BuildWriteInfo
    {
        // WriteCommands for all bundles
        public Dictionary<string, IWriteOperation> assetBundles = new Dictionary<string, IWriteOperation>();
        public Dictionary<string, List<IWriteOperation>> sceneBundles = new Dictionary<string, List<IWriteOperation>>();
    }
}
