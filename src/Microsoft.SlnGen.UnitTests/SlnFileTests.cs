// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Microsoft.SlnGen.UnitTests
{
    public class SlnFileTests : TestBase
    {
        [Fact(Skip = "Disabling for now, will fix platforms and configurations in future commit")]
        public void LotsOfProjects()
        {
            /*
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
                projects[i] = new SlnProject(
                    GetTempFileName(), $"Project{i:D6}", Guid.NewGuid(), Guid.NewGuid(), projectConfigurations, projectPlatforms, isMainProject: i == 0, isDeployable: false);
            }

            ValidateProjectInSolution(projects);
            */
        }

        [Fact]
        public void MultipleProjects()
        {
            SlnProject projectA = new SlnProject
            {
                FullPath = GetTempFileName(),
                Name = "ProjectA",
                ProjectGuid = new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"),
                ProjectTypeGuid = new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"),
                IsMainProject = true,
            };

            SlnProject projectB = new SlnProject
            {
                FullPath = GetTempFileName(),
                Name = "ProjectB",
                ProjectGuid = new Guid("EAD108BE-AC70-41E6-A8C3-450C545FDC0E"),
                ProjectTypeGuid = new Guid("F38341C3-343F-421A-AE68-94CD9ADCD32F"),
            };

            ValidateProjectInSolution(projectA, projectB);
        }

        [Fact]
        public void NoFolders()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectA",
                    ProjectGuid = new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"),
                    ProjectTypeGuid = new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"),
                    IsMainProject = true,
                },
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectB",
                    ProjectGuid = new Guid("EAD108BE-AC70-41E6-A8C3-450C545FDC0E"),
                    ProjectTypeGuid = new Guid("F38341C3-343F-421A-AE68-94CD9ADCD32F"),
                },
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectC",
                    ProjectGuid = new Guid("EDD837F8-48ED-45E1-BC77-6387EC6466AC"),
                    ProjectTypeGuid = new Guid("7C203CD8-314C-4358-AD5C-66152E899EAF"),
                },
            };

            ValidateProjectInSolution((slnProject, projectInSolution) => projectInSolution.ParentProjectGuid.ShouldBe(null), projects, false);
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
            SlnProject projectA = new SlnProject
            {
                FullPath = GetTempFileName(),
                Name = "ProjectA",
                ProjectGuid = new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"),
                ProjectTypeGuid = new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"),
                IsMainProject = true,
            };

            ValidateProjectInSolution(projectA);
        }

        [Fact]
        public void WithFolders()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectA",
                    ProjectGuid = new Guid("C95D800E-F016-4167-8E1B-1D3FF94CE2E2"),
                    ProjectTypeGuid = new Guid("88152E7E-47E3-45C8-B5D3-DDB15B2F0435"),
                    IsMainProject = true,
                },
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectB",
                    ProjectGuid = new Guid("F3CEBCAB-98E5-4041-84DB-033C9682F340"),
                    ProjectTypeGuid = new Guid("EEC9AD2B-9B7E-4581-864E-76A2BB607C3F"),
                    IsMainProject = true,
                },
                new SlnProject
                {
                    FullPath = GetTempFileName(),
                    Name = "ProjectC",
                    ProjectGuid = new Guid("0079D674-EC4D-4D09-9C4E-699D0D1B0F72"),
                    ProjectTypeGuid = new Guid("7717E4E9-5443-401B-A964-55727AF96E0C"),
                    IsMainProject = true,
                },
            };

            void Action(SlnProject slnProject, ProjectInSolution projectInSolution)
            {
                if (slnProject.IsMainProject)
                {
                    projectInSolution.ParentProjectGuid.ShouldBeNull();
                }
                else
                {
                    projectInSolution.ParentProjectGuid.ShouldNotBeNull();
                }
            }

            ValidateProjectInSolution(Action, projects, true);
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

                customValidator?.Invoke(slnProject, projectInSolution);
            }
        }

        private void ValidateProjectInSolution(params SlnProject[] projects)
        {
            ValidateProjectInSolution(null, projects, false);
        }
    }
}