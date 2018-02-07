using System;
using System.Collections.Generic;
using UnityEditor.Experimental.Build.AssetBundle;

namespace UnityEditor.Build.AssetBundle
{
    [Serializable]
    public class BuildResultInfo
    {
        public Dictionary<string, uint> bundleCRCs = new Dictionary<string, uint>();
        public Dictionary<string, List<WriteResult>> bundleResults = new Dictionary<string, List<WriteResult>>();
    }
}
