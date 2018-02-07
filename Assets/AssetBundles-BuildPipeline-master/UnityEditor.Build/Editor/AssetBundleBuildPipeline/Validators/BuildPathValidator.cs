using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Build.Utilities
{
    public static class BuildPathValidator
    {
        public const string kPathNotValidError = "Path: '{0}' is not a valid output folder for a build.";

        public static string[] kInvalidPaths = new[]
        {
            NormalizePath(Application.dataPath + "\\.."),
            NormalizePath(Application.dataPath),
            NormalizePath(Application.dataPath + "\\..\\Temp"),
            NormalizePath(Application.dataPath + "\\..\\ProjectSettings"),
            NormalizePath(Application.dataPath + "\\..\\Packages"),

            // TODO: Platform dependent checks
#if UNITY_EDITOR_WIN
#elif UNITY_EDITOR_OSX
#elif UNITY_EDITOR_LINUX
#endif
        };

        public static string[] kInvalidRegexPaths = new[]
        {
            NormalizePath(Application.dataPath + "\\..\\ProjectSettings") + "[\\/].*",
            NormalizePath(Application.dataPath + "\\..\\Packages") + "[\\/].*",

            // TODO: Platform dependent checks
#if UNITY_EDITOR_WIN
#elif UNITY_EDITOR_OSX
#elif UNITY_EDITOR_LINUX
#endif
        };

        private static string NormalizePath(string path)
        {
            // For sanity and Regex sake, we are normalizing using / in all cases
            var fullPath = Path.GetFullPath(path);
            if (Path.DirectorySeparatorChar == '/')
                return fullPath;
            return fullPath.Replace(Path.DirectorySeparatorChar, '/');
        }

        public static bool ValidOutputFolder(string outputFolder, bool logError)
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                if (logError)
                    BuildLogger.LogError(kPathNotValidError, outputFolder);
                return false;
            }

            var fullOutputPath = NormalizePath(outputFolder);
            foreach (var path in kInvalidPaths)
            {
                if (fullOutputPath == path)
                {
                    if (logError)
                        BuildLogger.LogError(kPathNotValidError, outputFolder);
                    return false;
                }
            }

            foreach (var path in kInvalidRegexPaths)
            {
                if (Regex.IsMatch(fullOutputPath, path))
                {
                    if (logError)
                        BuildLogger.LogError(kPathNotValidError, outputFolder);
                    return false;
                }
            }
            return true;
        }
    }
}