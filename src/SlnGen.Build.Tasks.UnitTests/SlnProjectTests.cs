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
        public void UseAssemblyNameProperty()
        {
            CreateAndValidateProject(expectedGuid: "3EA7B89C-F85F-49F4-B99D-1BC184C08186", expectedName: "Project.Name");
        }

        [Test]
        public void UseFileName()
        {
            CreateAndValidateProject(expectedGuid: "DE681393-7151-459D-862C-918CCD2CB371");
        }

        private SlnProject CreateAndValidateProject(bool isMainProject = false, string expectedGuid = null, string expectedName = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null)
        {
            string fullPath = GetTempFileName(extension);

            if (globalProperties == null)
            {
                globalProperties = new Dictionary<string, string>();
            }

            if (!String.IsNullOrWhiteSpace(expectedGuid))
            {
                globalProperties[SlnProject.ProjectGuidPropertyName] = expectedGuid;
            }

            if (!String.IsNullOrWhiteSpace(expectedName))
            {
                globalProperties[SlnProject.AssemblyNamePropertyName] = expectedName;
            }

            Project expectedProject = MockProject.Create(fullPath, globalProperties);

            SlnProject actualProject = SlnProject.FromProject(expectedProject, isMainProject);

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
    }
}