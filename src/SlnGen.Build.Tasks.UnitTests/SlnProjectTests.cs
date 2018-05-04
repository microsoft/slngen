// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public class SlnProjectTests : TestBase
    {
        [Fact]
        public void ConfigurationsAndPlatforms()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["Platform"] = "x64",
                ["Configuration"] = String.Empty
            };

            using (TestProject testProject = TestProject.Create(globalProperties))
            {
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, string>(), true);

                slnProject.Configurations.ShouldBe(new[] { "Debug", "Release" }, ignoreOrder: true);

                slnProject.Platforms.ShouldBe(new[] { "amd64", "AnyCPU", "x64" }, ignoreOrder: true);
            }
        }

        [Fact]
        public void ConfigurationsAndPlatformsWithGlobalProperties()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["Configuration"] = "Mix",
                ["Platform"] = "x86"
            };

            using (TestProject testProject = TestProject.Create(globalProperties))
            {
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, string>(), true);

                slnProject.Configurations.ShouldBe(new[] { "Debug", "Mix", "Release" }, ignoreOrder: true);

                slnProject.Platforms.ShouldBe(new[] { "amd64", "AnyCPU", "x86", }, ignoreOrder: true);
            }
        }

        [Fact]
        public void GetProjectGuidLegacyProjectSystem()
        {
            CreateAndValidateProject(expectedGuid: "{C69AC4B3-A0E1-40FC-94AB-C0F2E1F8D779}");
        }

        [Fact]
        public void GetProjectGuidSdkProject()
        {
            SlnProject actualProject = CreateAndValidateProject(globalProperties: new Dictionary<string, string>
            {
                { SlnProject.UsingMicrosoftNetSdkPropertyName, "true" }
            });

            actualProject.ProjectGuid.ShouldNotBeNull();

            Guid.TryParse(actualProject.ProjectGuid.ToSolutionString(), out _).ShouldBeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsMainProject(bool isMainProject)
        {
            CreateAndValidateProject(isMainProject: isMainProject);
        }

        [Fact]
        public void ShouldIncludeInSolutionExclusion()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { "IncludeInSolutionFile", "false" }
            };

            Project project = CreateProject("foo", ".csproj", globalProperties: globalProperties);

            SlnGen.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Fact]
        public void ShouldIncludeInSolutionTraversalProject()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { "IsTraversal", "true" }
            };

            Project project = CreateProject("dirs", ".proj", globalProperties: globalProperties);

            SlnGen.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Fact]
        public void UseAssemblyNameProperty()
        {
            CreateAndValidateProject(expectedGuid: "{3EA7B89C-F85F-49F4-B99D-1BC184C08186}", expectedName: "Project.Name");
        }

        [Fact]
        public void UseFileName()
        {
            CreateAndValidateProject(expectedGuid: "{DE681393-7151-459D-862C-918CCD2CB371}");
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
                actualProject.ProjectGuid.ToSolutionString().ShouldBe(expectedGuid);
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

        private sealed class TestProject : IDisposable
        {
            private const string TemplateProjectPath = @"TestFiles\SampleProject.csproj";

            private readonly Dictionary<string, string> _savedEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private TestProject(string fullPath, IDictionary<string, string> globalProperties)
            {
                SetEnvironmentVariables(new Dictionary<string, string>
                {
                    { "Configuration", null },
                    { "Platform", null },
                });

                Project = new Project(
                    fullPath ?? throw new ArgumentNullException(nameof(fullPath)),
                    globalProperties,
                    toolsVersion: null);
            }

            public Project Project { get; }

            public static TestProject Create(IDictionary<string, string> globalProperties = null)
            {
                string fullPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.proj");

                // Copy the template project to a temporary location
                File.Copy(Path.Combine(Environment.CurrentDirectory, TemplateProjectPath), fullPath);

                return new TestProject(fullPath, globalProperties);
            }

            public void Dispose()
            {
                if (File.Exists(Project.FullPath))
                {
                    File.Delete(Project.FullPath);
                }

                foreach (KeyValuePair<string, string> variable in _savedEnvironmentVariables)
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value, EnvironmentVariableTarget.Process);
                }
            }

            private void SetEnvironmentVariables(Dictionary<string, string> variables)
            {
                foreach (KeyValuePair<string, string> variable in variables)
                {
                    _savedEnvironmentVariables[variable.Key] = variable.Value;

                    Environment.SetEnvironmentVariable(variable.Key, variable.Value, EnvironmentVariableTarget.Process);
                }
            }
        }
    }
}