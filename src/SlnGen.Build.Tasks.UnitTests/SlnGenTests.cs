// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public sealed class SlnGenTests : TestBase
    {
        [Fact]
        public void ParseCustomProjectTypeGuidsDeduplicatesList()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(" .foo ")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, "1AB09E1B-77F6-4982-B020-374DB9DF2BD2" },
                },
                new MockTaskItem(".foo")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid.ToString() },
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
                    { SlnGen.CustomProjectTypeGuidMetadataName, "9d933978-2d2a-4fb2-b72d-8746d88e73b7" },
                },
                new MockTaskItem(".foo")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid.ToString() },
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void ProjectReferencesDeterminedInCrossTargetingBuild()
        {
            Dictionary<string, string> globlProperties = new Dictionary<string, string>
            {
                ["SlnGenLaunchVisualStudio"] = "false",
            };

            ProjectCollection projectCollection = new ProjectCollection(globlProperties);

            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.props"))
                .Save();
            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.targets"))
                .Import(Path.Combine(Environment.CurrentDirectory, "build", "SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' != 'true'")
                .Import(Path.Combine(Environment.CurrentDirectory, "buildCrossTargeting", "SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' == 'true'")
                .Save();

            ProjectCreator projectA = ProjectCreator.Templates
                .SdkCsproj(
                    Path.Combine(TestRootPath, "ProjectA", "ProjectA.csproj"),
                    targetFramework: "netcoreapp2.0")
                .Save();

            ProjectCreator projectB = ProjectCreator.Templates
                .SdkCsproj(
                    Path.Combine(TestRootPath, "ProjectB", "ProjectB.csproj"),
                    targetFramework: "net46")
                .Save();

            ProjectCreator projectC = ProjectCreator.Templates
                .SdkCsproj(
                    Path.Combine(TestRootPath, "ProjectC", "ProjectC.csproj"),
                    targetFramework: "net46")
                .ItemProjectReference(projectA)
                .Save();

            ProjectCreator projectD = ProjectCreator.Templates
                .SdkCsproj(
                    Path.Combine(TestRootPath, "ProjectD", "ProjectD.csproj"),
                    targetFramework: "net46")
                .ItemProjectReference(projectB)
                .ItemProjectReference(projectC)
                .Save();

            ProjectCreator
                .Create(
                    Path.Combine(TestRootPath, "ProjectC", "ProjectC.csproj"),
                    sdk: "Microsoft.NET.Sdk",
                    projectCollection: projectCollection,
                    projectFileOptions: NewProjectFileOptions.None)
                .PropertyGroup()
                .Property("TargetFrameworks", "net46;netcoreapp2.0")
                .Property("SlnGenAssemblyFile", Path.Combine(Environment.CurrentDirectory, "SlnGen.Build.Tasks.dll"))
                .ItemProjectReference(projectA, condition: "'$(TargetFramework)' == 'netcoreapp2.0'")
                .ItemProjectReference(projectB, condition: "'$(TargetFramework)' == 'net46'")
                .ItemProjectReference(projectC, condition: "'$(TargetFramework)' == 'net46'")
                .ItemProjectReference(projectD, condition: "'$(TargetFramework)' == 'net46'")
                .Save()
                .TryBuild("SlnGen", out bool result, out BuildOutput buildOutput, out IDictionary<string, TargetResult> targetOutputs);

            result.ShouldBeTrue(buildOutput.GetConsoleLog());

            KeyValuePair<string, TargetResult> targetOutput = targetOutputs.ShouldHaveSingleItem();

            targetOutput.Key.ShouldBe("SlnGen");

            targetOutput.Value.Items
                .Select(i => i.GetMetadata("OriginalItemSpec"))
                .ShouldBe(
                    new[]
                    {
                        projectA.FullPath,
                        projectB.FullPath,
                        projectC.FullPath,
                        projectD.FullPath,
                    },
                    ignoreOrder: true);
        }

        [Fact]
        public void SingleProject()
        {
            Dictionary<string, string> globlProperties = new Dictionary<string, string>
            {
                ["DesignTimeBuild"] = "true",
                ["SlnGenLaunchVisualStudio"] = "false",
            };

            ProjectCollection projectCollection = new ProjectCollection(globlProperties);

            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.props"))
                .Save();
            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.targets"))
                .Property("SlnGenAssemblyFile", Path.Combine(Environment.CurrentDirectory, "SlnGen.Build.Tasks.dll"))
                .Import(Path.Combine(Environment.CurrentDirectory, "build", "SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' != 'true'")
                .Import(Path.Combine(Environment.CurrentDirectory, "buildCrossTargeting", "SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' == 'true'")
                .Save();

            ProjectCreator projectA = ProjectCreator.Templates
                .SdkCsproj(
                    Path.Combine(TestRootPath, "ProjectA", "ProjectA.csproj"),
                    targetFramework: "netcoreapp2.0",
                    projectCollection: projectCollection)
                .Save()
                .TryBuild("SlnGen", out bool result, out BuildOutput buildOutput, out IDictionary<string, TargetResult> targetOutputs);

            result.ShouldBeTrue(buildOutput.GetConsoleLog());

            KeyValuePair<string, TargetResult> targetOutput = targetOutputs.ShouldHaveSingleItem();

            targetOutput.Key.ShouldBe("SlnGen");
        }

        [Fact]
        public void SolutionItems()
        {
            Dictionary<string, string> solutionItems = new Dictionary<string, string>
            {
                { "foo", Path.GetFullPath("foo") },
                { "bar", Path.GetFullPath("bar") },
            };

            IBuildEngine buildEngine = new MockBuildEngine();

            SlnGen slnGen = new SlnGen
            {
                BuildEngine = buildEngine,
                SolutionItems = solutionItems.Select(i => new MockTaskItem(i.Key)
                {
                    { "FullPath", i.Value }
                }).ToArray<ITaskItem>(),
            };

            slnGen.GetSolutionItems(path => true).ShouldBe(solutionItems.Values);
        }

        private static void ValidateParseCustomProjectTypeGuids(string fileExtension, string projectTypeGuid, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(fileExtension)
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, projectTypeGuid }
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, expectedFileExtension, expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(ITaskItem[] customProjectTypeGuids, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            SlnGen slnGen = new SlnGen
            {
                CustomProjectTypeGuids = customProjectTypeGuids,
            };

            Dictionary<string, Guid> actualProjectTypeGuids = slnGen.ParseCustomProjectTypeGuids();

            KeyValuePair<string, Guid> actualProjectTypeGuid = actualProjectTypeGuids.ShouldHaveSingleItem();

            actualProjectTypeGuid.Key.ShouldBe(expectedFileExtension);
            actualProjectTypeGuid.Value.ShouldBe(expectedProjectTypeGuid);
        }
    }
}