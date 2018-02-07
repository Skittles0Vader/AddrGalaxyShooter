namespace UnityEditor.Build
{
    public enum BuildPipelineCodes
    {
        // Success Codes are Positive!
        Success = 0,
        SuccessCached = 1,
        // Error Codes are Negative!
        Error = -1,
        Canceled = -2,
        UnsavedChanges = -3
    }
}
