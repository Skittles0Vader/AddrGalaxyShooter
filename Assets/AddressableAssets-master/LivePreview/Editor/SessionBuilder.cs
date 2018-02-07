using System.IO;
using UnityEditor;
using UnityEditor.Build;
//using UnityEditor.DatalessPlayer;
using UnityEditor.Experimental.Build.Player;
#if false //needs to be updated to lates AB-BP API

namespace AddressableAssets.LivePreview
{
    class SessionBuilder : IPostprocessBuild, IOrderedCallback
    {
        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (EditorUserBuildSettings.developmentDatalessPlayer)
            {
                Session session;
                session = new Session();
                session.sessionID = BuildPipeline.GetSessionIdForBuildTarget(target);
                session.buildTarget = target;
                session.buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
                ScriptCompilationSettings settings = new ScriptCompilationSettings();
                settings.options = ScriptCompilationOptions.DevelopmentBuild;
                settings.target = target;
                settings.group = session.buildTargetGroup;
                //settings.group = EditorUserBuildSettings.activeBuildTargetGroup;
                ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, FileUtil.GetUniqueTempPathInProject());
                session.typeDB = result.typeDB;
                session.Save();
            }
        }

        public int callbackOrder { get { return 0; } }
    }
}
#endif