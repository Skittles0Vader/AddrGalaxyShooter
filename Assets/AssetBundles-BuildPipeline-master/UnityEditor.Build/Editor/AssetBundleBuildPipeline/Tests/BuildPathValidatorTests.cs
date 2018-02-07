using System.IO;
using NUnit.Framework;
using UnityEditor.Build.Utilities;
using UnityEngine;

namespace UnityEditor.Build.Tests
{
    public class BuildPathValidatorTests
    {
        private string ProjectRoot { get { return Path.GetFullPath(Application.dataPath + "\\.."); } }

        [Test]
        public void NullIsInvalid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(null, false));
        }

        [Test]
        public void EmptyIsInvalid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder("", false));
        }

        [Test]
        public void ProjectRootIsInvalid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot, false));
        }

        [Test]
        public void TempRootIsInvalid_SubfoldersAreValid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Temp", false));
            Assert.IsTrue(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Temp\\Subfolder", false));
        }

        [Test]
        public void PackagesRootAndSubfoldersAreInvalid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Packages", false));
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Packages\\Subfolder", false));
        }

        [Test]
        public void ProjectSettingsRootAndSubFoldersAreInvalid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\ProjectSettings", false));
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\ProjectSettings\\Subfolder", false));
        }

        [Test]
        public void AssetsRootIsInvalid_SubfoldersAreValid()
        {
            Assert.IsFalse(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Assets", false));
            Assert.IsTrue(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Assets\\Subfolder", false));
        }

        [Test]
        public void UserFolderOffProjectRootIsValid()
        {
            Assert.IsTrue(BuildPathValidator.ValidOutputFolder(ProjectRoot + "\\Build", false));
        }
    }
}