// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using NUnit.Framework;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Linq;
using MSBuildSolutionFile = Microsoft.Build.Construction.SolutionFile;

namespace SlnGen.Build.Tasks.UnitTests
{
    [TestFixture]
    public class SlnFileTests : TestBase
    {
        // TODO: Test hierarchy
        [Test]
        public void LotsOfProjects()
        {
            const int projectCount = 1000;

            SlnProject[] projects = new SlnProject[projectCount];

            var configurations = new[] { "Debug", "Release" };
            var platforms = new[] { "x64", "x86", "Any CPU", "amd64" };

            var randomGenerator = new Random(Guid.NewGuid().GetHashCode());

            for (int i = 0; i < projectCount; i++)
            {
                // pick random and shuffled configurations and platforms
                var projectConfigurations = configurations.OrderBy(a => Guid.NewGuid()).Take(randomGenerator.Next(1, configurations.Length)).ToList();
                var projectPlatforms = platforms.OrderBy(a => Guid.NewGuid()).Take(randomGenerator.Next(1, platforms.Length)).ToList();
                projects[i] = new SlnProject(GetTempFileName(), $"Project{i:D6}", Guid.NewGuid(), Guid.NewGuid().ToSolutionString(), projectConfigurations, projectPlatforms, isMainProject: i == 0);
            }

            ValidateProjectInSolution(projects);
        }

        [Test]
        public void MultipleProjects()
        {
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", Guid.Parse("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"), "88152E7E-47E3-45C8-B5D3-DDB15B2F0435", new[] { "Debug" }, new[] { "x64" }, isMainProject: true);
            SlnProject projectB = new SlnProject(GetTempFileName(), "ProjectB", Guid.Parse("EAD108BE-AC70-41E6-A8C3-450C545FDC0E"), "F38341C3-343F-421A-AE68-94CD9ADCD32F", new[] { "Debug" }, new[] { "x64" }, isMainProject: false);

            ValidateProjectInSolution(projectA, projectB);
        }

        [Test]
        public void SingleProject()
        {
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", Guid.Parse("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"), "88152E7E-47E3-45C8-B5D3-DDB15B2F0435", new[] { "Debug" }, new[] { "x64" }, isMainProject: true);

            ValidateProjectInSolution(projectA);
        }

        private void ValidateProjectInSolution(Action<SlnProject, ProjectInSolution> customValidator, SlnProject[] projects)
        {
            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile(projects);

            slnFile.Save(solutionFilePath);

            MSBuildSolutionFile solutionFile = MSBuildSolutionFile.Parse(solutionFilePath);

            foreach (SlnProject slnProject in projects)
            {
                solutionFile.ProjectsByGuid.ContainsKey(slnProject.ProjectGuid.ToSolutionString()).ShouldBeTrue();

                ProjectInSolution projectInSolution = solutionFile.ProjectsByGuid[slnProject.ProjectGuid.ToSolutionString()];

                projectInSolution.AbsolutePath.ShouldBe(slnProject.FullPath);
                projectInSolution.ProjectGuid.ShouldBe(slnProject.ProjectGuid.ToSolutionString());
                projectInSolution.ProjectName.ShouldBe(slnProject.Name);

                var configurationPlatforms = from configuration in slnProject.Configurations from platform in slnProject.Platforms select $"{configuration}|{platform}";

                CollectionAssert.AreEquivalent(projectInSolution.ProjectConfigurations.Keys, configurationPlatforms);

                customValidator?.Invoke(slnProject, projectInSolution);
            }
        }

        private void ValidateProjectInSolution(params SlnProject[] projects)
        {
            ValidateProjectInSolution(null, projects);
        }
    }
}