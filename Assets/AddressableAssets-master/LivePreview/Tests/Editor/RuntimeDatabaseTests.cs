using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEditor.Experimental.Build.Player;

/*[TestFixture]
public class RuntimeDatabaseTests {

    [OneTimeSetUp]
    public static void OneTimeSetup()
    {
        ScriptCompilationSettings settings = new ScriptCompilationSettings();
        settings.outputFolder = FileUtil.GetUniqueTempPathInProject();
        Directory.CreateDirectory(settings.outputFolder);
        settings.target = EditorUserBuildSettings.activeBuildTarget;
        settings.targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        settings.options = ScriptCompilationOptions.DevelopmentBuild;
        ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings);

        // Create an editor side session
        Session session = SessionManager.CreateNewSession();
        session.buildTarget = EditorUserBuildSettings.activeBuildTarget;
        session.buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        session.typeDB = result.typeDB;
        RuntimeAssetDatabaseHandler handler = new RuntimeAssetDatabaseHandler();
        handler.Setup(session);
        //session.dataHandlers.Add(handler);



        // hook into the where the client sends requests through the network to the server
        // we can just just call the corresponding editor side function
        ClientSessionManager.SetRequestDataHandler(x => SessionManager.ProcesseMessage(x));
        ClientSessionManager.SetSessionId(session.sessionID.ToString());
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        //SessionManager.Instance.DeleteAllSessions();
    }

    [UnityTest]
    public static IEnumerator RemoteAssetDatabaseTest()
    {
        Material mat = RemoteAssetDatabase.LoadAssetAtPath<Material>("Assets/test.mat");
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.GetComponent<MeshRenderer>().material = mat;
        yield return null;
        Assert.AreEqual(1.0f, mat.color.r);
        Assert.AreEqual(0.0f, mat.color.g);
        Assert.AreEqual(0.0f, mat.color.b);

    }
}

[TestFixture]
public class TestTemp
{
    [Test]
    public static void MatTest()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/test.mat");
        Assert.AreEqual(1.0f, mat.color.r);
        Assert.AreEqual(0.0f, mat.color.g);
        Assert.AreEqual(0.0f, mat.color.b);
    }

    [Test]
    public static void MatBundleTest()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile("C:\\projects\\dlp\\dlp_simple\\TestAssetBundle\\matbundle");
        Material mat = bundle.LoadAsset<Material>("test");
        //Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/test.mat");
        Assert.AreEqual(1.0f, mat.color.r);
        Assert.AreEqual(1.0f, mat.color.g);
        Assert.AreEqual(0.0f, mat.color.b);
    }
}

public class BundleBuildClass
{
    [MenuItem("TestMe/BuildBundles")]
    public static void BuildBundles()
    {
        Directory.CreateDirectory("TestAssetBundle");
        BuildPipeline.BuildAssetBundles("TestAssetBundle", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }
}*/
