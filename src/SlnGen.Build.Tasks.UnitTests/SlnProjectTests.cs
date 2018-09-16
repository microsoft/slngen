// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
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
                ["Configuration"] = string.Empty
            };

            using (TestProject testProject = TestProject.Create(globalProperties))
            {
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, Guid>(), true);

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
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, Guid>(), true);

                slnProject.Configurations.ShouldBe(new[] { "Debug", "Mix", "Release" }, ignoreOrder: true);

                slnProject.Platforms.ShouldBe(new[] { "amd64", "AnyCPU", "x86", "x64" }, ignoreOrder: true);
            }
        }

        [Fact]
        public void GetProjectGuidLegacyProjectSystem()
        {
            SlnProject actualProject = CreateAndValidateProject(expectedGuid: "{C69AC4B3-A0E1-40FC-94AB-C0F2E1F8D779}");

            actualProject.ProjectTypeGuid.ShouldBe(SlnProject.DefaultLegacyProjectTypeGuid);
        }

        [Fact]
        public void GetProjectGuidSdkProject()
        {
            SlnProject actualProject = CreateAndValidateProject(globalProperties: new Dictionary<string, string>
            {
                [SlnProject.UsingMicrosoftNetSdkPropertyName] = "true"
            });

            actualProject.ProjectGuid.ShouldNotBeNull();

            Guid.TryParse(actualProject.ProjectGuid.ToSolutionString(), out _).ShouldBeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(".csproj")]
        [InlineData(".vbproj")]
        [InlineData(".fsproj")]
        [InlineData(".wixproj")]
        public void GetProjectTypeGuidLegacyProject(string extension)
        {
            SlnProject actualProject = CreateAndValidateProject(extension: extension);

            switch (extension)
            {
                case "":
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.DefaultLegacyProjectTypeGuid);
                    break;
                case ".csproj":
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"));
                    break;
                case ".vbproj":
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"));
                    break;
                default:
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.KnownProjectTypeGuids[extension]);
                    break;
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(".csproj")]
        [InlineData(".vbproj")]
        [InlineData(".fsproj")]
        [InlineData(".wixproj")]
        public void GetProjectTypeGuidSdkProject(string extension)
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                [SlnProject.UsingMicrosoftNetSdkPropertyName] = "true"
            };

            SlnProject actualProject = CreateAndValidateProject(globalProperties: globalProperties, extension: extension);

            switch (extension)
            {
                case "":
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.DefaultNetSdkProjectTypeGuid);
                    break;
                case ".csproj":
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556"));
                    break;
                case ".vbproj":
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9"));
                    break;
                default:
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.KnownProjectTypeGuids[extension]);
                    break;
            }
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
            CreateAndValidateProject(expectedGuid: "{3EA7B89C-F85F-49F4-B99D-1BC184C08186}");
        }

        [Fact]
        public void UseFileName()
        {
            CreateAndValidateProject(expectedGuid: "{DE681393-7151-459D-862C-918CCD2CB371}");
        }

        private SlnProject CreateAndValidateProject(bool isMainProject = false, string expectedGuid = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null)
        {
            Project expectedProject = CreateProject(expectedGuid, extension, globalProperties);

            SlnProject actualProject = SlnProject.FromProject(expectedProject, new Dictionary<string, Guid>(), isMainProject);

            actualProject.FullPath.ShouldBe(expectedProject.FullPath);

            actualProject.Name.ShouldBe(expectedProject.GetPropertyValue(SlnProject.MSBuildProjectNamePropertyName));

            if (expectedGuid != null)
            {
                actualProject.ProjectGuid.ToSolutionString().ShouldBe(expectedGuid);
            }

            actualProject.IsMainProject.ShouldBe(isMainProject);

            return actualProject;
        }

        private Project CreateProject(string projectGuid = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null)
        {
            string fullPath = GetTempFileName(extension);

            if (globalProperties == null)
            {
                globalProperties = new Dictionary<string, string>();
            }

            if (!string.IsNullOrWhiteSpace(projectGuid))
            {
                globalProperties[SlnProject.ProjectGuidPropertyName] = projectGuid;
            }

            return MockProject.Create(fullPath, globalProperties);
        }

        private sealed class TestProject : IDisposable
        {
            private readonly Dictionary<string, string> _savedEnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private TestProject()
            {
                SetEnvironmentVariables(new Dictionary<string, string>
                {
                    { "Configuration", null },
                    { "Platform", null },
                });
            }

            public Project Project { get; private set; }

            public static TestProject Create(IDictionary<string, string> globalProperties = null)
            {
                return new TestProject()
                {
                    Project = ProjectCreator.Templates.LegacyCsproj(
                        path: Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.proj"),
                        projectCollection: new ProjectCollection(globalProperties),
                        defaultPlatform: "x64",
                        projectCreator: projectCreator => projectCreator
                            .PropertyGroup(" '$(Configuration)|$(Platform)' == 'Release|amd64' ")
                            .PropertyGroup(" '$(Configuration)|$(Platform)' == 'Debug|x64' "))
                };
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