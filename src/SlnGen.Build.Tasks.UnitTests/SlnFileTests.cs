// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public class SlnFileTests : TestBase
    {
        [Fact]
        public void LotsOfProjects()
        {
            const int projectCount = 1000;

            SlnProject[] projects = new SlnProject[projectCount];

            string[] configurations = { "Debug", "Release" };
            string[] platforms = { "x64", "x86", "Any CPU", "amd64" };

            Random randomGenerator = new Random(Guid.NewGuid().GetHashCode());

            for (int i = 0; i < projectCount; i++)
            {
                // pick random and shuffled configurations and platforms
                List<string> projectConfigurations = configurations.OrderBy(a => Guid.NewGuid()).Take(randomGenerator.Next(1, configurations.Length)).ToList();
                List<string> projectPlatforms = platforms.OrderBy(a => Guid.NewGuid()).Take(randomGenerator.Next(1, platforms.Length)).ToList();
                projects[i] = new SlnProject(GetTempFileName(), $"Project{i:D6}", Guid.NewGuid(), Guid.NewGuid(), projectConfigurations, projectPlatforms, isMainProject: i == 0, isDeployable: false);
            }

            ValidateProjectInSolution(projects);
        }

        [Fact]
        public void MultipleProjects()
        {
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"), new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"), new[] { "Debug" }, new[] { "x64" }, isMainProject: true, isDeployable: false);
            SlnProject projectB = new SlnProject(GetTempFileName(), "ProjectB", new Guid("EAD108BE-AC70-41E6-A8C3-450C545FDC0E"), new Guid("F38341C3-343F-421A-AE68-94CD9ADCD32F"), new[] { "Debug" }, new[] { "x64" }, isMainProject: false, isDeployable: false);

            ValidateProjectInSolution(projectA, projectB);
        }

        [Fact]
        public void NoFolders()
        {
            SlnProject[] projects =
            {
                new SlnProject(GetTempFileName(), "ProjectA", new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"), new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"), new[] { "Debug" }, new[] { "x64" }, isMainProject: true, isDeployable: false),
                new SlnProject(GetTempFileName(), "ProjectB", new Guid("EAD108BE-AC70-41E6-A8C3-450C545FDC0E"), new Guid("F38341C3-343F-421A-AE68-94CD9ADCD32F"), new[] { "Debug" }, new[] { "x64" }, isMainProject: false, isDeployable: false),
                new SlnProject(GetTempFileName(), "ProjectC", new Guid("00C5B0C0-E19C-48C5-818D-E8CD4FA2A915"), new Guid("F38341C3-343F-421A-AE68-94CD9ADCD32F"), new[] { "Debug" }, new[] { "x64" }, isMainProject: false, isDeployable: false),
            };

            ValidateProjectInSolution((s, p) => p.ParentProjectGuid.ShouldBe(null), projects, false);
        }

        [Fact]
        public void SaveToCustomLocationCreatesDirectory()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(TestRootPath, "1", "2", "3"));

            directoryInfo.Exists.ShouldBeFalse();

            string fullPath = Path.Combine(directoryInfo.FullName, Path.GetRandomFileName());

            SlnFile slnFile = new SlnFile();

            slnFile.Save(fullPath, useFolders: false);

            File.Exists(fullPath).ShouldBeTrue();
        }

        [Fact]
        public void SingleProject()
        {
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"), new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"), new[] { "Debug" }, new[] { "x64" }, isMainProject: true, isDeployable: false);

            ValidateProjectInSolution(projectA);
        }

        private void ValidateProjectInSolution(Action<SlnProject, ProjectInSolution> customValidator, SlnProject[] projects, bool folders)
        {
            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile();

            slnFile.AddProjects(projects);
            slnFile.Save(solutionFilePath, folders);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            foreach (SlnProject slnProject in projects)
            {
                solutionFile.ProjectsByGuid.ContainsKey(slnProject.ProjectGuid.ToSolutionString()).ShouldBeTrue();

                ProjectInSolution projectInSolution = solutionFile.ProjectsByGuid[slnProject.ProjectGuid.ToSolutionString()];

                projectInSolution.AbsolutePath.ShouldBe(slnProject.FullPath);
                projectInSolution.ProjectGuid.ShouldBe(slnProject.ProjectGuid.ToSolutionString());
                projectInSolution.ProjectName.ShouldBe(slnProject.Name);

                IEnumerable<string> expected = slnProject.Configurations.SelectMany(configuration => slnProject.Platforms, (configuration, platform) => $"{configuration}|{platform}");
                IEnumerable<string> actual = projectInSolution.ProjectConfigurations.Where(i => i.Value.IncludeInBuild).Select(i => i.Key);

                expected.ShouldBe(actual, ignoreOrder: true);

                customValidator?.Invoke(slnProject, projectInSolution);
            }
        }

        private void ValidateProjectInSolution(params SlnProject[] projects)
        {
            ValidateProjectInSolution(null, projects, false);
        }
    }
}