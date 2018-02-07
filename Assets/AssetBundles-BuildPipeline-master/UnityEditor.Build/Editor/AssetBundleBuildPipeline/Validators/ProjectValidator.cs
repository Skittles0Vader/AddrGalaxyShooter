using Boo.Lang;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.Build.Utilities
{
    public static class ProjectValidator
    {
        public static bool HasDirtyScenes()
        {
            var unsavedChanges = false;
            var sceneCount = EditorSceneManager.sceneCount;
            for (var i = 0; i < sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                    continue;
                unsavedChanges = true;
                break;
            }

            return unsavedChanges;
        }

        public static void SaveDirtyScenes()
        {
            var scenes = new List<Scene>();
            var sceneCount = EditorSceneManager.sceneCount;
            for (var i = 0; i < sceneCount; ++i)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                    continue;
                scenes.Add(scene);
            }
            
            EditorSceneManager.SaveScenes(scenes.ToArray());
        }
    }
}
