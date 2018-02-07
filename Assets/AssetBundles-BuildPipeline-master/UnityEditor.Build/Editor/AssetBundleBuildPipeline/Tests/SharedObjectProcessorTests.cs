using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.Build.AssetBundle;
using UnityEditor.Build.AssetBundle.DataConverters;
using UnityEditor.Build.Utilities;
using UnityEditor.Experimental.Build.AssetBundle;
using UnityEngine;

namespace UnityEditor.Build.Tests
{
    public class SharedObjectProcessorTests
    {
        private SharedObjectProcessor processor;

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            processor = new SharedObjectProcessor(false, null);
        }

        [Test]
        public void NonAggressiveSharesOnlyReferencesObjects()
        {
            // Given an input of 2 Prefabs pointing to the same Mesh, and only prefabs are assigned to bundles
            // NonAggressive mode should generate 1 virtual asset containing the mesh and monoscript
            var dependency = TestDataGenerators.CreateAssetsWithFBXMeshReference(false);
            var settings = BundleBuildPipeline.GenerateBundleBuildSettings(null);
            var exitCode = processor.Convert(dependency, settings, false, out dependency);

            var prefab1 = new GUID("00000000000000000000000000000001");
            var prefab2 = new GUID("00000000000000000000000000000002");

            var virtualAsset = new GUID("21000000360000004b000000d3000000");
            var virtualObject1 = dependency.assetInfo[prefab1].referencedObjects[0];    // Mesh
            var virtualObject2 = dependency.assetInfo[prefab1].referencedObjects[1];    // MonoScript

            // Ensure processor returns Success
            Assert.AreEqual(BuildPipelineCodes.Success, exitCode);

            // Ensure we created AssetLoadInfos for virtualAsset 1 & 2
            AssetLoadInfo vaInfo;
            Assert.IsTrue(dependency.assetInfo.TryGetValue(virtualAsset, out vaInfo));
            Assert.AreEqual(2, vaInfo.includedObjects.Count);
            Assert.AreEqual(virtualObject1, vaInfo.includedObjects[0]);
            Assert.AreEqual(virtualObject2, vaInfo.includedObjects[1]);
            Assert.AreEqual(virtualAsset, vaInfo.asset);
            Assert.AreEqual(virtualAsset.ToString(), vaInfo.address);


            // Ensure we created dependency lists for the new virtual assets
            List<string> assetDependencies;
            Assert.IsTrue(dependency.assetToBundles.TryGetValue(virtualAsset, out assetDependencies));
            Assert.AreEqual(1, assetDependencies.Count);
            Assert.AreEqual(virtualAsset.ToString(), assetDependencies[0]);


            // Ensure we updated the dependency lists for the existing assets
            Assert.IsTrue(dependency.assetToBundles.TryGetValue(prefab1, out assetDependencies));
            Assert.AreEqual(2, assetDependencies.Count);
            Assert.AreEqual(prefab1.ToString(), assetDependencies[0]);
            Assert.AreEqual(virtualAsset.ToString(), assetDependencies[1]);

            Assert.IsTrue(dependency.assetToBundles.TryGetValue(prefab2, out assetDependencies));
            Assert.AreEqual(2, assetDependencies.Count);
            Assert.AreEqual(prefab2.ToString(), assetDependencies[0]);
            Assert.AreEqual(virtualAsset.ToString(), assetDependencies[1]);


            // Ensure we updated the asset lists for bundles
            List<GUID> assetsInBundle;
            Assert.IsTrue(dependency.bundleToAssets.TryGetValue(virtualAsset.ToString(), out assetsInBundle));
            Assert.AreEqual(1, assetsInBundle.Count);
            Assert.AreEqual(virtualAsset, assetsInBundle[0]);
        }

        [Test]
        public void AggressiveSharesIncludedAndReferencesObjects()
        {
            // Given an input of 2 Prefabs pointing to the same Mesh, and all 3 assets are assigned to bundles
            // Aggressive mode should generate 2 virtual assets
            // 1 - Mesh, needed for all 3 bundles
            // 2 - MonoScript, needed for prefab bundles
            var dependency = TestDataGenerators.CreateAssetsWithFBXMeshReference(true);
            var settings = BundleBuildPipeline.GenerateBundleBuildSettings(null);
            var exitCode = processor.Convert(dependency, settings, true, out dependency);

            var prefab1 = new GUID("00000000000000000000000000000001");
            var prefab2 = new GUID("00000000000000000000000000000002");
            var fbx = new GUID("00000000000000000000000000000010");

            var virtualAsset1 = new GUID("ee000000c8000000a2000000aa000000");
            var virtualObject1 = dependency.assetInfo[prefab1].referencedObjects[0];    // Mesh

            var virtualAsset2 = new GUID("21000000360000004b000000d3000000");
            var virtualObject2 = dependency.assetInfo[prefab1].referencedObjects[1];    // MonoScript

            // Ensure processor returns Success
            Assert.AreEqual(BuildPipelineCodes.Success, exitCode);

            // Ensure we created AssetLoadInfos for virtualAsset 1 & 2
            AssetLoadInfo vaInfo1;
            Assert.IsTrue(dependency.assetInfo.TryGetValue(virtualAsset1, out vaInfo1));
            Assert.AreEqual(1, vaInfo1.includedObjects.Count);
            Assert.AreEqual(virtualObject1, vaInfo1.includedObjects[0]);
            Assert.AreEqual(virtualAsset1, vaInfo1.asset);
            Assert.AreEqual(virtualAsset1.ToString(), vaInfo1.address);

            AssetLoadInfo vaInfo2;
            Assert.IsTrue(dependency.assetInfo.TryGetValue(virtualAsset2, out vaInfo2));
            Assert.AreEqual(1, vaInfo2.includedObjects.Count);
            Assert.AreEqual(virtualObject2, vaInfo2.includedObjects[0]);
            Assert.AreEqual(virtualAsset2, vaInfo2.asset);
            Assert.AreEqual(virtualAsset2.ToString(), vaInfo2.address);


            // Ensure we created dependency lists for the new virtual assets
            List<string> assetDependencies;
            Assert.IsTrue(dependency.assetToBundles.TryGetValue(virtualAsset1, out assetDependencies));
            Assert.AreEqual(1, assetDependencies.Count);
            Assert.AreEqual(virtualAsset1.ToString(), assetDependencies[0]);

            Assert.IsTrue(dependency.assetToBundles.TryGetValue(virtualAsset2, out assetDependencies));
            Assert.AreEqual(1, assetDependencies.Count);
            Assert.AreEqual(virtualAsset2.ToString(), assetDependencies[0]);


            // Ensure we updated the dependency lists for the existing assets
            Assert.IsTrue(dependency.assetToBundles.TryGetValue(prefab1, out assetDependencies));
            Assert.AreEqual(4, assetDependencies.Count);
            Assert.AreEqual(prefab1.ToString(), assetDependencies[0]);
            Assert.AreEqual(fbx.ToString(), assetDependencies[1]);
            Assert.AreEqual(virtualAsset1.ToString(), assetDependencies[2]);
            Assert.AreEqual(virtualAsset2.ToString(), assetDependencies[3]);

            Assert.IsTrue(dependency.assetToBundles.TryGetValue(prefab2, out assetDependencies));
            Assert.AreEqual(4, assetDependencies.Count);
            Assert.AreEqual(prefab2.ToString(), assetDependencies[0]);
            Assert.AreEqual(fbx.ToString(), assetDependencies[1]);
            Assert.AreEqual(virtualAsset1.ToString(), assetDependencies[2]);
            Assert.AreEqual(virtualAsset2.ToString(), assetDependencies[3]);

            Assert.IsTrue(dependency.assetToBundles.TryGetValue(fbx, out assetDependencies));
            Assert.AreEqual(2, assetDependencies.Count);
            Assert.AreEqual(fbx.ToString(), assetDependencies[0]);
            Assert.AreEqual(virtualAsset1.ToString(), assetDependencies[1]);


            // Ensure we updated the asset lists for bundles
            List<GUID> assetsInBundle;
            Assert.IsTrue(dependency.bundleToAssets.TryGetValue(virtualAsset1.ToString(), out assetsInBundle));
            Assert.AreEqual(1, assetsInBundle.Count);
            Assert.AreEqual(virtualAsset1, assetsInBundle[0]);

            Assert.IsTrue(dependency.bundleToAssets.TryGetValue(virtualAsset2.ToString(), out assetsInBundle));
            Assert.AreEqual(1, assetsInBundle.Count);
            Assert.AreEqual(virtualAsset2, assetsInBundle[0]);
        }

        //[Test]
        // Can't be tested with fake assets, need to figure out a better method
        public void VirtualAssetsHaveCalculatedDependencies()
        {
            var dependency = TestDataGenerators.CreateAssetsWithMaterialReference();
            var settings = BundleBuildPipeline.GenerateBundleBuildSettings(null);
            var exitCode = processor.Convert(dependency, settings, true, out dependency);

            var virtualAsset1 = new GUID("10000000e2000000640000007a000000");
            var virtualAsset2 = new GUID("e9000000af00000074000000d8000000");

            // Ensure processor returns Success
            Assert.AreEqual(BuildPipelineCodes.Success, exitCode);

            // Ensure we updated the dependency lists for the virtual assets
            List<string> assetDependencies;
            Assert.IsTrue(dependency.assetToBundles.TryGetValue(virtualAsset1, out assetDependencies));
            Assert.AreEqual(2, assetDependencies.Count);
            Assert.AreEqual(virtualAsset1.ToString(), assetDependencies[0]);
            Assert.AreEqual(virtualAsset2.ToString(), assetDependencies[1]);

            Assert.IsTrue(dependency.assetToBundles.TryGetValue(virtualAsset2, out assetDependencies));
            Assert.AreEqual(1, assetDependencies.Count);
            Assert.AreEqual(virtualAsset2.ToString(), assetDependencies[0]);
        }
    }
}