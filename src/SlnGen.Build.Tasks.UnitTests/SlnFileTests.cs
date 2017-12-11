using Microsoft.Build.Construction;
using NUnit.Framework;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using MSBuildSolutionFile = Microsoft.Build.Construction.SolutionFile;

namespace SlnGen.Build.Tasks.UnitTests
{
    [TestFixture]
    public class SlnFileTests : TestBase
    {
        // TODO: Test hierarchy
        // TODO: Test configurations and platforms

        [Test]
        public void LotsOfProjects()
        {
            const int projectCount = 1000;

            SlnProject[] projects = new SlnProject[projectCount];

            for (int i = 0; i < projectCount; i++)
            {
                projects[i] = new SlnProject(GetTempFileName(), $"Project{i:D6}", Guid.NewGuid().ToSolutionString(), Guid.NewGuid().ToSolutionString(), isMainProject: i == 0);
            }

            ValidateProjectInSolution(projects);
        }

        [Test]
        public void MultipleProjects()
        {
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", "C95D800E-F016-4167-8E1B-1D3FF94CE2E2", "88152E7E-47E3-45C8-B5D3-DDB15B2F0435", isMainProject: true);
            SlnProject projectB = new SlnProject(GetTempFileName(), "ProjectB", "EAD108BE-AC70-41E6-A8C3-450C545FDC0E", "F38341C3-343F-421A-AE68-94CD9ADCD32F", isMainProject: false);

            ValidateProjectInSolution(projectA, projectB);
        }

        [Test]
        public void SingleProject()
        {
            // TODO: Get this working
            SlnProject projectA = new SlnProject(GetTempFileName(), "ProjectA", "C95D800E-F016-4167-8E1B-1D3FF94CE2E2", "88152E7E-47E3-45C8-B5D3-DDB15B2F0435", isMainProject: true);

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
                solutionFile.ProjectsByGuid.ContainsKey(slnProject.ProjectGuid).ShouldBeTrue();

                ProjectInSolution projectInSolution = solutionFile.ProjectsByGuid[slnProject.ProjectGuid];

                projectInSolution.AbsolutePath.ShouldBe(slnProject.FullPath);
                projectInSolution.ProjectGuid.ShouldBe(slnProject.ProjectGuid);
                projectInSolution.ProjectName.ShouldBe(slnProject.Name);

                customValidator?.Invoke(slnProject, projectInSolution);
            }
        }

        private void ValidateProjectInSolution(params SlnProject[] projects)
        {
            ValidateProjectInSolution(null, projects);
        }
    }
}