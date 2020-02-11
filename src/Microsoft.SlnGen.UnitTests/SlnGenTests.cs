﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.SlnGen.UnitTests
{
    public sealed class SlnGenTests : TestBase
    {
#if NETFRAMEWORK
        [Fact]
#endif
        public void ProjectReferencesDeterminedInCrossTargetingBuild()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["SlnGenLaunchVisualStudio"] = "false",
            };

            ProjectCollection projectCollection = new ProjectCollection(globalProperties);

            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.props"))
                .Save();
            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.targets"))
                .Import(Path.Combine(Environment.CurrentDirectory, "build", "Microsoft.SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' != 'true'")
                .Import(Path.Combine(Environment.CurrentDirectory, "buildMultiTargeting", "Microsoft.SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' == 'true'")
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

            ProjectCreator mainProject = ProjectCreator
                .Create(
                    Path.Combine(TestRootPath, "ProjectE", "ProjectE.csproj"),
                    sdk: "Microsoft.NET.Sdk",
                    projectCollection: projectCollection,
                    projectFileOptions: NewProjectFileOptions.None)
                .PropertyGroup()
                .Property("TargetFrameworks", "net46;netcoreapp2.0")
                .Property("SlnGenAssemblyFile", Path.Combine(Environment.CurrentDirectory, "slngen.exe"))
                .ItemProjectReference(projectA, condition: "'$(TargetFramework)' == 'netcoreapp2.0'")
                .ItemProjectReference(projectB, condition: "'$(TargetFramework)' == 'net46'")
                .ItemProjectReference(projectC, condition: "'$(TargetFramework)' == 'net46'")
                .ItemProjectReference(projectD, condition: "'$(TargetFramework)' == 'net46'")
                .Save()
                .TryBuild("SlnGen", out bool result, out BuildOutput buildOutput, out IDictionary<string, TargetResult> targetOutputs);

            result.ShouldBeTrue(buildOutput.GetConsoleLog());

            string expectedSolutionFilePath = Path.ChangeExtension(mainProject.FullPath, ".sln");

            File.Exists(expectedSolutionFilePath).ShouldBeTrue();

            SolutionFile solutionFile = SolutionFile.Parse(expectedSolutionFilePath);

            solutionFile.ProjectsInOrder.Select(i => i.AbsolutePath).ShouldBe(new string[]
            {
                projectA,
                projectB,
                projectC,
                projectD,
                mainProject,
            });
        }

#if NETFRAMEWORK

        [Fact]
#endif
        public void SingleProject()
        {
            Dictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                [MSBuildPropertyNames.DesignTimeBuild] = bool.TrueString,
                [MSBuildPropertyNames.SlnGenLaunchVisualStudio] = bool.FalseString,
            };

            ProjectCollection projectCollection = new ProjectCollection(globalProperties);

            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.props"))
                .Save();
            ProjectCreator
                .Create(Path.Combine(TestRootPath, "Directory.Build.targets"))
                .Property("SlnGenAssemblyFile", Path.Combine(Environment.CurrentDirectory, "slngen.exe"))
                .Import(Path.Combine(Environment.CurrentDirectory, "build", "Microsoft.SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' != 'true'")
                .Import(Path.Combine(Environment.CurrentDirectory, "buildMultiTargeting", "Microsoft.SlnGen.targets"), condition: "'$(IsCrossTargetingBuild)' == 'true'")
                .Save();

            ProjectCreator.Templates
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
    }
}