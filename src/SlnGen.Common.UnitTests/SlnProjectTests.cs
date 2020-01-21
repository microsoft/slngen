// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using SlnGen.UnitTests.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Common.UnitTests
{
    public class SlnProjectTests : TestBase
    {
        [Fact]
        public void ConfigurationsAndPlatforms()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["Platform"] = "x64",
                ["Configuration"] = string.Empty,
            };

            using (TestProject testProject = TestProject.Create(globalProperties))
            {
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, Guid>(), true);

                slnProject.Configurations.ShouldBe(new[] { "Debug", "Release" }, ignoreOrder: true);

                slnProject.Platforms.ShouldBe(new[] { "amd64", "Any CPU", "x64" }, ignoreOrder: true);
            }
        }

        [Fact]
        public void ConfigurationsAndPlatformsWithGlobalProperties()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["Configuration"] = "Mix",
                ["Platform"] = "x86",
            };

            using (TestProject testProject = TestProject.Create(globalProperties))
            {
                SlnProject slnProject = SlnProject.FromProject(testProject.Project, new Dictionary<string, Guid>(), true);

                slnProject.Configurations.ShouldBe(new[] { "Debug", "Mix", "Release" }, ignoreOrder: true);

                slnProject.Platforms.ShouldBe(new[] { "amd64", "Any CPU", "x86", "x64" }, ignoreOrder: true);
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
                [SlnConstants.UsingMicrosoftNETSdk] = "true",
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
        [InlineData(".sfproj")]
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
                [SlnConstants.UsingMicrosoftNETSdk] = "true",
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
        [InlineData("true", ".csproj", true)]
        [InlineData("false", ".csproj", false)]
        [InlineData(null, ".csproj", false)]
        [InlineData(null, ".sfproj", true)]
        [InlineData("false", ".sfproj", false)]
        public void IsDeployable(string isDeployable, string projectExtension, bool expected)
        {
            SlnProject project = CreateAndValidateProject(extension: projectExtension, isDeployable: isDeployable);

            project.IsDeployable.ShouldBe(expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsMainProject(bool isMainProject)
        {
            CreateAndValidateProject(isMainProject: isMainProject);
        }

        [Fact]
        public void ParseCustomProjectTypeGuidsDeduplicatesList()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(" .foo ")
                {
                    { SlnConstants.ProjectTypeGuid, "1AB09E1B-77F6-4982-B020-374DB9DF2BD2" },
                },
                new MockTaskItem(".foo")
                {
                    { SlnConstants.ProjectTypeGuid, expectedProjectTypeGuid.ToString() },
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void ParseCustomProjectTypeGuidsFormatsFileExtensionAndGuid()
        {
            ValidateParseCustomProjectTypeGuids(
                " .FoO  ",
                "  9d9339782d2a4fb2b72d8746d88e73b7 ",
                ".foo",
                new Guid("9D933978-2D2A-4FB2-B72D-8746D88E73B7"));
        }

        [Fact]
        public void ParseCustomProjectTypeGuidsIgnoresNonFileExtensions()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem("foo")
                {
                    { SlnConstants.ProjectTypeGuid, "9d933978-2d2a-4fb2-b72d-8746d88e73b7" },
                },
                new MockTaskItem(".foo")
                {
                    { SlnConstants.ProjectTypeGuid, expectedProjectTypeGuid.ToString() },
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void ShouldIncludeInSolutionExclusion()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { "IncludeInSolutionFile", "false" },
            };

            Project project = CreateProject("foo", ".csproj", globalProperties: globalProperties);

            SlnProject.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Fact]
        public void ShouldIncludeInSolutionTraversalProject()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { "IsTraversal", "true" },
            };

            Project project = CreateProject("dirs", ".proj", globalProperties: globalProperties);

            SlnProject.ShouldIncludeInSolution(project).ShouldBeFalse();
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

        private static void ValidateParseCustomProjectTypeGuids(string fileExtension, string projectTypeGuid, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(fileExtension)
                {
                    { SlnConstants.ProjectTypeGuid, projectTypeGuid },
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, expectedFileExtension, expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(ITaskItem[] customProjectTypeGuids, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            Dictionary<string, Guid> actualProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(customProjectTypeGuids.Select(i => new MSBuildTaskItem(i)));

            KeyValuePair<string, Guid> actualProjectTypeGuid = actualProjectTypeGuids.ShouldHaveSingleItem();

            actualProjectTypeGuid.Key.ShouldBe(expectedFileExtension);
            actualProjectTypeGuid.Value.ShouldBe(expectedProjectTypeGuid);
        }

        private SlnProject CreateAndValidateProject(bool isMainProject = false, string expectedGuid = null, string expectedName = null, string extension = ".csproj", IDictionary<string, string> globalProperties = null, string isDeployable = null)
        {
            if (!string.IsNullOrWhiteSpace(isDeployable))
            {
                if (globalProperties == null)
                {
                    globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                globalProperties["SlnGenIsDeployable"] = isDeployable;
            }

            Project expectedProject = CreateProject(expectedGuid, expectedName, extension, globalProperties);

            SlnProject actualProject = SlnProject.FromProject(expectedProject, new Dictionary<string, Guid>(), isMainProject);

            actualProject.FullPath.ShouldBe(expectedProject.FullPath);

            if (!string.IsNullOrWhiteSpace(expectedName))
            {
                actualProject.Name.ShouldBe(expectedName);
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

            if (!string.IsNullOrWhiteSpace(projectGuid))
            {
                globalProperties[SlnConstants.ProjectGuid] = projectGuid;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                globalProperties[SlnConstants.AssemblyName] = name;
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
                            .PropertyGroup(" '$(Configuration)|$(Platform)' == 'Debug|x64' ")),
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