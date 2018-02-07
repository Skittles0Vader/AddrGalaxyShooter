using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using AddressableAssets.LivePreview;

#if false //needs to be updated to lates AB-BP API

[TestFixture]
public class BundleBuilderTests
{
    private const string kTestAssetDir = "Assets/RemoteAssetDatabase/Tests/";
    //private const string kTestAssetDir = "Assets/";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        /*ScriptCompilationSettings settings = new ScriptCompilationSettings();
        settings.outputFolder = FileUtil.GetUniqueTempPathInProject();
        Directory.CreateDirectory(settings.outputFolder);
        settings.target = EditorUserBuildSettings.activeBuildTarget;
        settings.targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        settings.options = ScriptCompilationOptions.DevelopmentBuild;
        ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings);*/
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        //SessionManager.Instance.DeleteAllSessions();
    }

    GUID TestAssetToGUID(string assetName)
    {
        string path = kTestAssetDir + assetName;
        string stringGuid = AssetDatabase.AssetPathToGUID(path);
        return new GUID(stringGuid);
    }

    private List<AssetBundle> mBundles;
    private LivePreviewBuilder mBundleMgr;

    [SetUp]
    public void SetUp()
    {
        mBundles = new List<AssetBundle>();
        string cacheFolder = FileUtil.GetUniqueTempPathInProject();
        mBundleMgr = new LivePreviewBuilder(EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.selectedBuildTargetGroup, null, cacheFolder);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (AssetBundle bundle in mBundles)
            bundle.Unload(true);
        mBundles.Clear();
    }

    public AssetBundle LoadBundle(string path)
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle != null)
            mBundles.Add(bundle);
        return bundle;
    }

    public AssetBundle LoadBundle(BundleIdentifier id)
    {
        return LoadBundle(mBundleMgr.GetAbsoluteBundlePath(id));
    }

    public AssetBundle[] LoadBundles(string[] paths)
    {
        AssetBundle[] bundles = new AssetBundle[paths.Length];
        for (int i = 0; i < paths.Length; i++)
            bundles[i] = LoadBundle(paths[i]);
        return bundles;
    }

    public AssetBundle[] LoadBundles(BundleIdentifier[] ids)
    {
        AssetBundle[] bundles = new AssetBundle[ids.Length];
        for (int i = 0; i < ids.Length; i++)
            bundles[i] = LoadBundle(ids[i]);
        return bundles;
    }

    [Test]
    public void Material_WithCustomShader_CanLoad()
    {
        GUID guid = TestAssetToGUID("mat_with_custom_shader.mat");

        BundleIdentifier[] bundleIds = mBundleMgr.GetBundlesForGUID(guid);
        AssetBundle[] bundles = LoadBundles(bundleIds);
        Material mat = bundles[0].LoadAsset<Material>("main");

        Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f), mat.color);
    }

    [Test]
    public void Material_WithBuiltInShader_CanLoad()
    {
        GUID guid = TestAssetToGUID("mat_with_builtin_shader.mat");

        BundleIdentifier[] bundlePaths = mBundleMgr.GetBundlesForGUID(guid);
        AssetBundle[] bundles = LoadBundles(bundlePaths);
        Material mat = bundles[0].LoadAsset<Material>("main");

        Assert.AreEqual(new Color(1.0f, 0.0f, 0.0f), mat.color);
    }

    [Test]
    public void PrefabMaterialShader_HasThreeBundlesAndLoads()
    {
        GUID guid = TestAssetToGUID("PrefabWithCustomMat.prefab");

        BundleIdentifier[] bundlePaths = mBundleMgr.GetBundlesForGUID(guid);
        AssetBundle[] bundles = LoadBundles(bundlePaths);
        GameObject prefab = bundles[0].LoadAsset<GameObject>("main");

        Assert.NotNull(prefab);
        Assert.AreEqual(4, bundles.Length); // The shader is pulling in something from unity extra for some reason.
    }

    [Test]
    public void PrefabMaterialShader_PrefabRootIsMainAsset()
    {
        GUID guid = TestAssetToGUID("PrefabRoot.prefab");

        BundleIdentifier[] bundlePaths = mBundleMgr.GetBundlesForGUID(guid);
        AssetBundle[] bundles = LoadBundles(bundlePaths);
        GameObject prefab = bundles[0].LoadAsset<GameObject>("main");

        Assert.NotNull(prefab);
        Assert.AreEqual("PrefabRoot", prefab.name);
    }

    [Test]
    public void PartialBundles_CanHaveExternalReferences()
    {
        GUID guid = TestAssetToGUID("p1.prefab");

        BundleIdentifier[] bundlePaths = mBundleMgr.GetBundlesForGUID(guid);
        AssetBundle[] bundles = LoadBundles(bundlePaths);
        GameObject prefab = bundles[0].LoadAsset<GameObject>("main");

        refscript rs1 = prefab.GetComponent<refscript>();
        Assert.NotNull(rs1);
        Assert.NotNull(rs1.refObject);

        refscript rs2 = rs1.refObject.GetComponent<refscript>();
        Assert.NotNull(rs2);
        Assert.NotNull(rs2.refObject);

        Assert.NotNull(rs2.refObject);
    }
}

#endif