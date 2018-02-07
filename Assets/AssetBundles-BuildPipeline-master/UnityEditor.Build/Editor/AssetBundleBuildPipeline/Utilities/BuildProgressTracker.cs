using System;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public class BuildProgressTracker : IProgressTracker, IDisposable
    {
        public int StepCount { get; set; }

        public int ProgressCount { get; set; }

        private int m_CurrentStep = 0;

        private int m_CurrentProgress = 0;

        private string m_StepTitle;

        public BuildProgressTracker(int stepCount)
        {
            StepCount = Mathf.Max(stepCount, 1);
        }

        public void StartStep(string title, int progressCount)
        {
            m_CurrentStep++;
            m_StepTitle = string.Format("{0} ({1} of {2})", title, m_CurrentStep, StepCount);
            ProgressCount = Mathf.Max(progressCount, 1);
            m_CurrentProgress = 0;
            UpdateProgress("");
        }

        public bool UpdateProgress(string info)
        {
            float progress = (float)m_CurrentProgress / (float)ProgressCount;
            m_CurrentProgress++;
            return !EditorUtility.DisplayCancelableProgressBar(m_StepTitle, info, progress);
        }

        public bool EndProgress()
        {
            return !EditorUtility.DisplayCancelableProgressBar(m_StepTitle, "", 1f);
        }

        public void ClearTracker()
        {
            EditorUtility.ClearProgressBar();
        }

        public void Dispose()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
