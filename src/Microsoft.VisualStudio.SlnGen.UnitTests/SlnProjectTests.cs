﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class SlnProjectTests : TestBase
    {
        /*
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
        */

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
                [MSBuildPropertyNames.UsingMicrosoftNETSdk] = bool.TrueString,
            });

            actualProject.ProjectGuid.ShouldNotBe(default);

            Guid.TryParse(actualProject.ProjectGuid.ToSolutionString(), out _).ShouldBeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(ProjectFileExtensions.CSharp)]
        [InlineData(ProjectFileExtensions.VisualBasic)]
        [InlineData(ProjectFileExtensions.FSharp)]
        [InlineData(ProjectFileExtensions.Wix)]
        [InlineData(ProjectFileExtensions.SqlServerDb)]
        [InlineData(ProjectFileExtensions.AzureServiceFabric)]
        [InlineData(ProjectFileExtensions.Scope)]
        public void GetProjectTypeGuidLegacyProject(string extension)
        {
            SlnProject actualProject = CreateAndValidateProject(extension: extension);

            switch (extension)
            {
                case "":
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.DefaultLegacyProjectTypeGuid);
                    break;

                case ProjectFileExtensions.CSharp:
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"));
                    break;

                case ProjectFileExtensions.VisualBasic:
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"));
                    break;

                default:
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.KnownProjectTypeGuids[extension]);
                    break;
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(ProjectFileExtensions.CSharp)]
        [InlineData(ProjectFileExtensions.VisualBasic)]
        [InlineData(ProjectFileExtensions.FSharp)]
        [InlineData(ProjectFileExtensions.Wix)]
        [InlineData(ProjectFileExtensions.SqlServerDb)]
        public void GetProjectTypeGuidSdkProject(string extension)
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                [MSBuildPropertyNames.UsingMicrosoftNETSdk] = bool.TrueString,
            };

            SlnProject actualProject = CreateAndValidateProject(globalProperties: globalProperties, extension: extension);

            switch (extension)
            {
                case "":
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.DefaultNetSdkProjectTypeGuid);
                    break;

                case ProjectFileExtensions.CSharp:
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556"));
                    break;

                case ProjectFileExtensions.VisualBasic:
                    actualProject.ProjectTypeGuid.ShouldBe(new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9"));
                    break;

                default:
                    actualProject.ProjectTypeGuid.ShouldBe(SlnProject.KnownProjectTypeGuids[extension]);
                    break;
            }
        }

        [Theory]
        [InlineData("true", ProjectFileExtensions.CSharp, true)]
        [InlineData("false", ProjectFileExtensions.CSharp, false)]
        [InlineData(null, ProjectFileExtensions.CSharp, false)]
        [InlineData(null, ProjectFileExtensions.AzureServiceFabric, true)]
        [InlineData("false", ProjectFileExtensions.AzureServiceFabric, false)]
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
        public void ParseCustomProjectTypeGuidsDeDuplicatesList()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            Project project = ProjectCreator.Create()
                .ItemInclude(
                    MSBuildItemNames.SlnGenCustomProjectTypeGuid,
                    " .foo ",
                    metadata: new Dictionary<string, string>
                    {
                        { MSBuildPropertyNames.ProjectTypeGuid, "1AB09E1B-77F6-4982-B020-374DB9DF2BD2" },
                    })
                .ItemInclude(
                    MSBuildItemNames.SlnGenCustomProjectTypeGuid,
                    ".foo",
                    metadata: new Dictionary<string, string>
                    {
                        { MSBuildPropertyNames.ProjectTypeGuid, expectedProjectTypeGuid.ToString() },
                    });

            ValidateParseCustomProjectTypeGuids(project, ".foo", expectedProjectTypeGuid);
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

            Project project = ProjectCreator.Create()
                .ItemInclude(
                    MSBuildItemNames.SlnGenCustomProjectTypeGuid,
                    "foo",
                    metadata: new Dictionary<string, string>
                    {
                        { MSBuildPropertyNames.ProjectTypeGuid, "CF61A759-0062-4271-9D96-7891B75EEAD7" },
                    })
                .ItemInclude(
                    MSBuildItemNames.SlnGenCustomProjectTypeGuid,
                    ".foo",
                    metadata: new Dictionary<string, string>
                    {
                        { MSBuildPropertyNames.ProjectTypeGuid, expectedProjectTypeGuid.ToString() },
                    });

            ValidateParseCustomProjectTypeGuids(project, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void ShouldIncludeInSolutionExclusion()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { MSBuildPropertyNames.IncludeInSolutionFile, bool.FalseString },
            };

            Project project = CreateProject("foo", ProjectFileExtensions.CSharp, globalProperties: globalProperties);

            SlnProject.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Fact]
        public void ShouldIncludeInSolutionTraversalProject()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                { MSBuildPropertyNames.IsTraversal, bool.TrueString },
            };

            Project project = CreateProject("dirs", ".proj", globalProperties: globalProperties);

            SlnProject.ShouldIncludeInSolution(project).ShouldBeFalse();
        }

        [Fact]
        public void SolutionItems()
        {
            TestLogger logger = new TestLogger();

            string[] items =
            {
                Path.Combine(TestRootPath, "foo"),
                Path.Combine(TestRootPath, "bar"),
            };

            Project project = ProjectCreator.Create(
                    path: Path.Combine(TestRootPath, "foo.proj"))
                .ItemInclude(MSBuildItemNames.SlnGenSolutionItem, "foo")
                .ItemInclude(MSBuildItemNames.SlnGenSolutionItem, "bar");

            SlnProject.GetSolutionItems(project, logger, path => true).ShouldBe(items);

            logger.LowImportanceMessages.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("SomeProject")]
        public void ProjectNameIsCorrect(string projectName)
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                [MSBuildPropertyNames.SlnGenProjectName] = projectName ?? string.Empty,
            };

            SlnProject project = CreateAndValidateProject(expectedGuid: "{3EA7B89C-F85F-49F4-B99D-1BC184C08186}", globalProperties: globalProperties);

            if (!string.IsNullOrWhiteSpace(projectName))
            {
                project.Name.ShouldBe(projectName);
            }
            else
            {
                project.Name.ShouldBe(Path.GetFileNameWithoutExtension(project.FullPath));
            }
        }

        [Fact]
        public void GetPlatformsAndConfigurationsFromCppProject()
        {
            Project project = ProjectCreator.Create()
                .ItemInclude(
                    "ProjectConfiguration",
                    "ConfigA|PlatformA",
                    metadata: new Dictionary<string, string>
                    {
                        ["Configuration"] = "ConfigA",
                        ["Platform"] = "PlatformA",
                    })
                .ItemInclude(
                    "ProjectConfiguration",
                    "ConfigA|PlatformB",
                    metadata: new Dictionary<string, string>
                    {
                        ["Configuration"] = "ConfigA",
                        ["Platform"] = "PlatformB",
                    })
                .ItemInclude(
                    "ProjectConfiguration",
                    "ConfigB|PlatformA",
                    metadata: new Dictionary<string, string>
                    {
                        ["Configuration"] = "ConfigB",
                        ["Platform"] = "PlatformA",
                    })
                .ItemInclude(
                    "ProjectConfiguration",
                    "ConfigB|PlatformB",
                    metadata: new Dictionary<string, string>
                    {
                        ["Configuration"] = "ConfigB",
                        ["Platform"] = "PlatformB",
                    })
                .Save(GetTempFileName(".vcxproj"));

            SlnProject slnProject = SlnProject.FromProject(project, new Dictionary<string, Guid>());

            slnProject.Configurations.ShouldBe(new[] { "ConfigA", "ConfigB" });
            slnProject.Platforms.ShouldBe(new[] { "PlatformA", "PlatformB" });
        }

        [Fact]
        public void UseFileName()
        {
            CreateAndValidateProject(expectedGuid: "{DE681393-7151-459D-862C-918CCD2CB371}");
        }

        private static void ValidateParseCustomProjectTypeGuids(string fileExtension, string projectTypeGuid, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            Project project = ProjectCreator.Create()
                .ItemInclude(
                    MSBuildItemNames.SlnGenCustomProjectTypeGuid,
                    fileExtension,
                    metadata: new Dictionary<string, string>
                    {
                        { MSBuildPropertyNames.ProjectTypeGuid, projectTypeGuid },
                    });

            ValidateParseCustomProjectTypeGuids(project, expectedFileExtension, expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(Project project, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            IReadOnlyDictionary<string, Guid> actualProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(project);

            KeyValuePair<string, Guid> actualProjectTypeGuid = actualProjectTypeGuids.ShouldHaveSingleItem();

            actualProjectTypeGuid.Key.ShouldBe(expectedFileExtension);
            actualProjectTypeGuid.Value.ShouldBe(expectedProjectTypeGuid);
        }

        private SlnProject CreateAndValidateProject(bool isMainProject = false, string expectedGuid = null, string extension = ProjectFileExtensions.CSharp, IDictionary<string, string> globalProperties = null, string isDeployable = null)
        {
            if (!isDeployable.IsNullOrWhiteSpace())
            {
                globalProperties ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                globalProperties[MSBuildPropertyNames.SlnGenIsDeployable] = isDeployable;
            }

            Project expectedProject = CreateProject(expectedGuid, extension, globalProperties);

            SlnProject actualProject = SlnProject.FromProject(expectedProject, new Dictionary<string, Guid>(), isMainProject);

            actualProject.FullPath.ShouldBe(expectedProject.FullPath);

            if (expectedGuid != null)
            {
                actualProject.ProjectGuid.ToSolutionString().ShouldBe(expectedGuid);
            }

            actualProject.IsMainProject.ShouldBe(isMainProject);

            return actualProject;
        }

        private Project CreateProject(string projectGuid = null, string extension = ProjectFileExtensions.CSharp, IDictionary<string, string> globalProperties = null)
        {
            string fullPath = GetTempFileName(extension);

            globalProperties ??= new Dictionary<string, string>();

            if (!projectGuid.IsNullOrWhiteSpace())
            {
                globalProperties[MSBuildPropertyNames.ProjectGuid] = projectGuid;
            }

            return MockProject.Create(fullPath, globalProperties);
        }
    }
}