using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using AddressableAssets;
using UnityEditor;

namespace AddressableAssets.Tests
{
    public abstract class AddressableAssetTestBase
    {
        protected const string TestConfigName = "AddresableAssetSettings";
        protected const string TestConfigFolder = "Assets/AddressableAssetsData_AddressableAssetSettingsTests";
        protected AddressableAssetSettings settings;
        protected string assetGUID;

        [OneTimeSetUp]
        public void Init()
        {
            AssetDatabase.DeleteAsset(TestConfigFolder);
            settings = AddressableAssetSettings.GetDefault(true, false, TestConfigFolder, TestConfigName);
            settings.labelTable.labelNames.Clear();
            GameObject testObject = new GameObject("TestObject");
            PrefabUtility.CreatePrefab(TestConfigFolder + "/test.prefab", testObject);
            assetGUID = AssetDatabase.AssetPathToGUID(TestConfigFolder + "/test.prefab");
            OnInit();
        }

        protected virtual void OnInit() { }

        [OneTimeTearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(TestConfigFolder);
            OnCleanup();
        }

        protected virtual void OnCleanup() { }
    }
}