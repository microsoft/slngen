// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class SlnFileTests : TestBase
    {
        [Fact]
        public void CustomConfigurationAndPlatforms()
        {
            SlnProject projectA = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectA",
                Platforms = new[] { "AnyCPU", "x64", "x86" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectB = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectB",
                Platforms = new[] { "x64", "x86" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectC = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectC",
                Platforms = new[] { "amd64" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectD = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectD",
                Platforms = new[] { "Razzle" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectE = new SlnProject
            {
                Configurations = new[] { "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectE",
                Platforms = new[] { "AnyCPU" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnFile slnFile = new SlnFile()
            {
                Configurations = new[] { "Debug" },
                Platforms = new[] { "Any CPU" },
            };

            slnFile.AddProjects(new[] { projectA, projectB, projectC, projectD, projectE });

            string solutionFilePath = GetTempFileName();

            slnFile.Save(solutionFilePath, useFolders: false);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            ValidateSolutionPlatformAndConfiguration(projectA, solutionFile, "Debug", "AnyCPU");

            ValidateSolutionPlatformAndConfiguration(projectB, solutionFile, "Debug", "x64");

            ValidateSolutionPlatformAndConfiguration(projectC, solutionFile, "Debug", "amd64");

            ValidateSolutionPlatformAndConfiguration(projectD, solutionFile, "Debug", "Razzle", expectedIncludeInBuild: false);

            ValidateSolutionPlatformAndConfiguration(projectE, solutionFile, "Release", "AnyCPU", expectedIncludeInBuild: false);
        }

        [Fact]
        public void CustomConfigurationAndPlatforms_IgnoresInvalidValues()
        {
            SlnProject project = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectA",
                Platforms = new[] { "AnyCPU" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnFile slnFile = new SlnFile()
            {
                Configurations = new[] { "Debug" },
                Platforms = new[] { "Any CPU", "AnyCPU", "Invalid", "x64", "x86", "X64", "X86" },
            };

            slnFile.AddProjects(new[] { project });

            string solutionFilePath = GetTempFileName();

            slnFile.Save(solutionFilePath, useFolders: false);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            solutionFile.SolutionConfigurations
                .Select(i => i.FullName)
                .ShouldBe(
                    new[]
                    {
                        "Debug|Any CPU",
                        "Debug|x64",
                        "Debug|x86",
                    });

            ProjectInSolution projectInSolution = solutionFile.ProjectsByGuid[project.ProjectGuid.ToSolutionString()];

            projectInSolution.AbsolutePath.ShouldBe(project.FullPath);

            projectInSolution.ProjectConfigurations.Keys.ShouldBe(new[] { "Debug|Any CPU", "Debug|x64", "Debug|x86" });

            ValidateSolutionPlatformAndConfiguration(projectInSolution.ProjectConfigurations, "Debug|Any CPU", "Debug", "AnyCPU");
            ValidateSolutionPlatformAndConfiguration(projectInSolution.ProjectConfigurations, "Debug|x64", "Debug", "AnyCPU");
            ValidateSolutionPlatformAndConfiguration(projectInSolution.ProjectConfigurations, "Debug|x86", "Debug", "AnyCPU");
        }

        [Fact]
        public void CustomConfigurationAndPlatforms_MapsAnyCPU()
        {
            SlnProject projectA = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectA",
                Platforms = new[] { "AnyCPU" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectB = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectB",
                Platforms = new[] { "x64", "x86" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectC = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectC",
                Platforms = new[] { "amd64" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnProject projectD = new SlnProject
            {
                Configurations = new[] { "Debug", "Release" },
                FullPath = GetTempFileName(),
                IsMainProject = true,
                Name = "ProjectD",
                Platforms = new[] { "Razzle" },
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
            };

            SlnFile slnFile = new SlnFile()
            {
                Configurations = new[] { "Debug" },
                Platforms = new[] { "x64" },
            };

            slnFile.AddProjects(new[] { projectA, projectB, projectC, projectD });

            string solutionFilePath = GetTempFileName();

            slnFile.Save(solutionFilePath, useFolders: false);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            ValidateSolutionPlatformAndConfiguration(projectA, solutionFile, "Debug", "AnyCPU");

            ValidateSolutionPlatformAndConfiguration(projectB, solutionFile, "Debug", "x64");

            ValidateSolutionPlatformAndConfiguration(projectC, solutionFile, "Debug", "amd64");

            ValidateSolutionPlatformAndConfiguration(projectD, solutionFile, "Debug", "Razzle", expectedIncludeInBuild: false);
        }

        [Fact]
        public void ExistingSolutionIsReused()
        {
            string path = GetTempFileName();

            Guid projectGuid = Guid.Parse("7BE5A5CA-169D-4955-AB4D-EDDE662F4AE5");

            SlnProject project = new SlnProject
            {
                FullPath = GetTempFileName(),
                Name = "Project",
                ProjectGuid = Guid.NewGuid(),
                ProjectTypeGuid = Guid.NewGuid(),
                IsMainProject = true,
            };

            SlnFile slnFile = new SlnFile
            {
                ExistingProjectGuids = new Dictionary<string, Guid>
                {
                    [project.FullPath] = projectGuid,
                },
                SolutionGuid = Guid.NewGuid(),
            };

            slnFile.AddProjects(new[] { project });

            slnFile.Save(path, useFolders: false);

            SlnFile.TryParseExistingSolution(path, out Guid solutionGuid, out _).ShouldBeTrue();

            solutionGuid.ShouldBe(slnFile.SolutionGuid);

            SolutionFile solutionFile = SolutionFile.Parse(path);

            ProjectInSolution projectInSolution = solutionFile.ProjectsInOrder.ShouldHaveSingleItem();

            projectInSolution.ProjectGuid.ShouldBe(projectGuid.ToString("B").ToUpperInvariant());
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
        public void TryParseExistingSolution()
        {
            FileInfo solutionFilePath = new FileInfo(GetTempFileName());

            FileInfo projectFilePath = new FileInfo(Path.Combine(solutionFilePath.DirectoryName!, @"src\Microsoft.VisualStudio.SlnGen\Microsoft.VisualStudio.SlnGen.csproj"));

            Directory.CreateDirectory(projectFilePath.DirectoryName!);

            File.WriteAllText(projectFilePath.FullName, @"<Project />");

            File.WriteAllText(
                solutionFilePath.FullName,
                @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29011.400
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Microsoft.VisualStudio.SlnGen"", ""src\Microsoft.VisualStudio.SlnGen\Microsoft.VisualStudio.SlnGen.csproj"", ""{C8D336E5-9E65-4D34-BA9A-DB58936947CF}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C8D336E5-9E65-4D34-BA9A-DB58936947CF}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C8D336E5-9E65-4D34-BA9A-DB58936947CF}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C8D336E5-9E65-4D34-BA9A-DB58936947CF}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C8D336E5-9E65-4D34-BA9A-DB58936947CF}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {CFFC4187-96EE-4465-B5B3-0BAFD3C14BB6}
	EndGlobalSection
EndGlobal
");
            SlnFile.TryParseExistingSolution(solutionFilePath.FullName, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath).ShouldBeTrue();

            solutionGuid.ShouldBe(Guid.Parse("CFFC4187-96EE-4465-B5B3-0BAFD3C14BB6"));

            projectGuidsByPath.ShouldBe(new List<KeyValuePair<string, Guid>>
            {
                new KeyValuePair<string, Guid>(projectFilePath.FullName, Guid.Parse("{C8D336E5-9E65-4D34-BA9A-DB58936947CF}")),
            });
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

            static void Action(SlnProject slnProject, ProjectInSolution projectInSolution)
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

        [Fact]
        public void WithFoldersIgnoreMainProject()
        {
            string root = Path.GetTempPath();
            string projectName1 = Path.GetFileName(Path.GetTempFileName());
            string projectName2 = Path.GetFileName(Path.GetTempFileName());
            string projectName3 = Path.GetFileName(Path.GetTempFileName());
            Project[] projects =
            {
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder1", "Project1", projectName1),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder1", "Project2", projectName2),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder2", "Project3", projectName3),
                },
            };

            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile();

            slnFile.AddProjects(projects, new Dictionary<string, Guid>());
            slnFile.Save(solutionFilePath, true);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            foreach (ProjectInSolution slnProject in solutionFile.ProjectsInOrder)
            {
                if (slnProject.ProjectName == projectName1)
                {
                    slnProject.ParentProjectGuid.ShouldNotBeNull();
                }
                else if (slnProject.ProjectName == projectName2)
                {
                    slnProject.ParentProjectGuid.ShouldNotBeNull();
                }
                else if (slnProject.ProjectName == projectName3)
                {
                    slnProject.ParentProjectGuid.ShouldNotBeNull();
                }
            }
        }

        [Fact]
        public void WithFoldersDoNotIgnoreMainProject()
        {
            string root = Path.GetTempPath();
            string projectName1 = Path.GetFileName(Path.GetTempFileName());
            string projectName2 = Path.GetFileName(Path.GetTempFileName());
            string projectName3 = Path.GetFileName(Path.GetTempFileName());
            Project[] projects =
            {
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder1", "Project1", projectName1),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder1", "Project2", projectName2),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder2", "Project3", projectName3),
                },
            };

            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile();

            slnFile.AddProjects(projects, new Dictionary<string, Guid>(), projects[1].FullPath);
            slnFile.Save(solutionFilePath, true);

            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);

            foreach (ProjectInSolution slnProject in solutionFile.ProjectsInOrder)
            {
                if (slnProject.ProjectName == projectName1)
                {
                    slnProject.ParentProjectGuid.ShouldBeNull();
                }
                else if (slnProject.ProjectName == projectName2)
                {
                    slnProject.ParentProjectGuid.ShouldNotBeNull();
                }
                else if (slnProject.ProjectName == projectName3)
                {
                    slnProject.ParentProjectGuid.ShouldNotBeNull();
                }
            }
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

        private void ValidateSolutionPlatformAndConfiguration(SlnProject project, SolutionFile solutionFile, string expectedConfiguration, string expectedPlatform, bool expectedIncludeInBuild = true)
        {
            ProjectInSolution projectInSolution = solutionFile.ProjectsByGuid[project.ProjectGuid.ToSolutionString()];

            projectInSolution.AbsolutePath.ShouldBe(project.FullPath);

            ProjectConfigurationInSolution projectConfigurationInSolution = projectInSolution.ProjectConfigurations.ShouldHaveSingleItem().Value;

            projectConfigurationInSolution.ConfigurationName.ShouldBe(expectedConfiguration);
            projectConfigurationInSolution.PlatformName.ShouldBe(expectedPlatform);
            projectConfigurationInSolution.IncludeInBuild.ShouldBe(expectedIncludeInBuild);
        }

        private void ValidateSolutionPlatformAndConfiguration(IReadOnlyDictionary<string, ProjectConfigurationInSolution> projectConfigurationsInSolution, string key, string expectedConfiguration, string expectedPlatform, bool expectedIncludeInBuild = true)
        {
            projectConfigurationsInSolution.TryGetValue(key, out ProjectConfigurationInSolution projectConfigurationInSolution).ShouldBeTrue();

            projectConfigurationInSolution!.ConfigurationName.ShouldBe(expectedConfiguration);
            projectConfigurationInSolution.PlatformName.ShouldBe(expectedPlatform);
            projectConfigurationInSolution.IncludeInBuild.ShouldBe(expectedIncludeInBuild);
        }
    }
}