using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public interface IProgressTracker
    {
        int StepCount { get; set; }

        int ProgressCount { get; set; }

        void StartStep(string title, int progressCount);

        bool UpdateProgress(string info);

        bool EndProgress();

        void ClearTracker();
    }
}
