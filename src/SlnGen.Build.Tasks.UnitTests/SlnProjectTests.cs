using Microsoft.Build.Evaluation;
using NUnit.Framework;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;

namespace SlnGen.Build.Tasks.UnitTests
{
    [TestFixture]
    public class SlnProjectTests : TestBase
    {
        private const string TestProjectPath = @"TestFiles\SampleProject.csproj";

        [Test]
        public void GetProjectGuidLegacyProjectSystem()
        {
            CreateAndValidateProject(expectedGuid: "C69AC4B3-A0E1-40FC-94AB-C0F2E1F8D779");
        }

        [Test]
        public void GetProjectGuidSdkProject()
        {
            SlnProject actualProject = CreateAndValidateProject(globalProperties: new Dictionary<string, string>
            {
                {SlnProject.UsingMicrosoftNetSdkPropertyName, "true"}
            });

            actualProject.ProjectGuid.ShouldNotBeNull();

            Guid.TryParse(actualProject.ProjectGuid, out _).ShouldBeTrue();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void IsMainProject(bool isMainProject)
        {
            CreateAndValidateProject(isMainProject: isMainProject);
        }

        [Test]
        public void ShouldIncludeInSolutionExclusion()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                {"IncludeInSolutionFile", "false"}
            };

            Project project = CreateProject("foo", ".csproj", globalProperties: globalProperties);

            SlnGen.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Test]
        public void ShouldIncludeInSolutionTraversalProject()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                {"IsTraversal", "true"}
            };

            Project project = CreateProject("dirs", ".proj", globalProperties: globalProperties);

            SlnGen.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Test]
        public void UseAssemblyNameProperty()
        {
            CreateAndValidateProject(expectedGuid: "3EA7B89C-F85F-49F4-B99D-1BC184C08186", expectedName: "Project.Name");
        }

        [Test]
        public void UseFileName()
        {
            CreateAndValidateProject(expectedGuid: "DE681393-7151-459D-862C-918CCD2CB371");
        }

        [Test]
        public void ConfigurationsAndPlatforms()
        {
            Project project = new Project(TestProjectPath);

            SlnProject slnProject = SlnProject.FromProject(project, new Dictionary<string, string>(), true);

            IEnumerable<string> expectedConfigurations = new[] { "Debug", "Release" };
            IEnumerable<string> expectedPlatforms = new[] { "amd64", "x64", "AnyCPU" };

            CollectionAssert.AreEquivalent(expectedConfigurations, slnProject.Configurations);
            CollectionAssert.AreEquivalent(expectedPlatforms, slnProject.Platforms);
        }

        [Test]
        public void ConfigurationsAndPlatformsWithGlobalProperties()
        {
            Project project = new Project(
                TestProjectPath, 
                new Dictionary<string, string>
                {
                    ["Configuration"] = "Mix",
                    ["Platform"] = "x86"
                }, 
                "12.0");

            SlnProject slnProject = SlnProject.FromProject(project, new Dictionary<string, string>(), true);

            IEnumerable<string> expectedConfigurations = new[] { "Mix", "Debug", "Release" };
            IEnumerable<string> expectedPlatforms = new[] { "amd64", "AnyCPU", "x86" };

            CollectionAssert.AreEquivalent(expectedConfigurations, slnProject.Configurations);
            CollectionAssert.AreEquivalent(expectedPlatforms, slnProject.Platforms);
        }

        private SlnProject CreateAndValidateProject(bool isMainProject = false, string expectedGuid = null, string expectedName = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null)
        {
            Project expectedProject = CreateProject(expectedGuid, expectedName, extension, globalProperties);

            SlnProject actualProject = SlnProject.FromProject(expectedProject, new Dictionary<string, string>(), isMainProject);

            actualProject.FullPath.ShouldBe(expectedProject.FullPath);

            if (!String.IsNullOrWhiteSpace(expectedName))
            {
                actualProject.Name.ShouldBe(expectedName);
            }

            if (!String.IsNullOrWhiteSpace(extension))
            {
                actualProject.ProjectTypeGuid.ShouldBe(SlnProject.KnownProjectTypeGuids.ContainsKey(extension) ? SlnProject.KnownProjectTypeGuids[extension] : SlnProject.DefaultProjectTypeGuid);
            }

            if (expectedGuid != null)
            {
                actualProject.ProjectGuid.ShouldBe(expectedGuid);
            }

            actualProject.IsMainProject.ShouldBe(isMainProject);

            return actualProject;
        }

        private Project CreateProject(string projectGuid = null, string name = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null)
        {
            string fullPath = GetTempFileName(extension);

            if (globalProperties == null)
            {
                globalProperties = new Dictionary<string, string>();
            }

            if (!String.IsNullOrWhiteSpace(projectGuid))
            {
                globalProperties[SlnProject.ProjectGuidPropertyName] = projectGuid;
            }

            if (!String.IsNullOrWhiteSpace(name))
            {
                globalProperties[SlnProject.AssemblyNamePropertyName] = name;
            }

            return MockProject.Create(fullPath, globalProperties);
        }
    }
}