// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
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

            ValidateSolutionPlatformAndConfiguration(projectE, solutionFile, "Release", "AnyCPU", expectedIncludeInBuild: true);
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
                    [Path.GetFileName(project.FullPath)] = projectGuid,
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
        public void PathsWorkForAllDirectorySeparatorChars()
        {
            const string solutionText = @"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""ProjectA"", ""ProjectA\ProjectA.csproj"", ""{E859E866-96F9-474E-A1EA-6539385AD236}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""ProjectB"", ""ProjectB\ProjectB.csproj"", ""{893607F9-C204-4CB2-8BF2-1F71B4198CD2}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""ProjectC"", ""ProjectC\ProjectC.csproj"", ""{081A3445-4E74-4AE9-95B6-FF564FE70CA3}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{E859E866-96F9-474E-A1EA-6539385AD236}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{E859E866-96F9-474E-A1EA-6539385AD236}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{E859E866-96F9-474E-A1EA-6539385AD236}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{E859E866-96F9-474E-A1EA-6539385AD236}.Release|Any CPU.Build.0 = Release|Any CPU
		{893607F9-C204-4CB2-8BF2-1F71B4198CD2}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{893607F9-C204-4CB2-8BF2-1F71B4198CD2}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{893607F9-C204-4CB2-8BF2-1F71B4198CD2}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{893607F9-C204-4CB2-8BF2-1F71B4198CD2}.Release|Any CPU.Build.0 = Release|Any CPU
		{081A3445-4E74-4AE9-95B6-FF564FE70CA3}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{081A3445-4E74-4AE9-95B6-FF564FE70CA3}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{081A3445-4E74-4AE9-95B6-FF564FE70CA3}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{081A3445-4E74-4AE9-95B6-FF564FE70CA3}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {AA784BCF-D76D-4DD7-91D2-79E6A14A4DE2}
	EndGlobalSection
EndGlobal
";

            string solutionFileFullPath = GetTempFileName(".sln");

            File.WriteAllText(solutionFileFullPath, solutionText);

            ProgramArguments programArguments = new ProgramArguments
            {
                LaunchVisualStudio = new[] { bool.FalseString },
                SolutionFileFullPath = new[] { solutionFileFullPath },
            };

            Project[] projects =
            {
                ProjectCreator.Templates.SdkCsproj(path: GetTempProjectFile("ProjectA"))
                    .Save(),
                ProjectCreator.Templates.SdkCsproj(path: GetTempProjectFile("ProjectB"))
                    .Save(),
                ProjectCreator.Templates.SdkCsproj(path: GetTempProjectFile("ProjectC"))
                    .Save(),
            };

            TestLogger testLogger = new TestLogger();

            (string _, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid) = SlnFile.GenerateSolutionFile(programArguments, projects, testLogger);

            string actualSolutionText = File.ReadAllText(solutionFileFullPath);

            actualSolutionText.ShouldBe(solutionText, StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void ProjectConfigurationPlatformOrderingSameAsProjects()
        {
            const string projectTypeGuid = "{7E0F1516-6200-48BD-83FC-3EFA3AB4A574}";

            SlnFile slnFile = new SlnFile
            {
                SolutionGuid = new Guid("00CB211B-D6BE-49C6-AF57-2604ECCD9E72"),
            };

            slnFile.AddProjects(new[]
            {
                new SlnProject
                {
                    FullPath = Path.Combine(TestRootPath, "C", "C.csproj"),
                    Name = "C",
                    ProjectGuid = new Guid("0CCA75AE-ED20-431E-8853-B9F54333E87A"),
                    ProjectTypeGuid = new Guid(projectTypeGuid),
                    IsMainProject = true,
                    Configurations = new[]
                    {
                        "Debug",
                        "Release",
                    },
                    Platforms = new[]
                    {
                        "AnyCPU",
                    },
                },
                new SlnProject
                {
                    FullPath = Path.Combine(TestRootPath, "B", "B.csproj"),
                    Name = "B",
                    ProjectGuid = new Guid("0CCA75AE-ED20-431E-8853-B9F54333E87A"),
                    ProjectTypeGuid = new Guid(projectTypeGuid),
                    Configurations = new[]
                    {
                        "Debug",
                        "Release",
                    },
                    Platforms = new[]
                    {
                        "AnyCPU",
                    },
                },
                new SlnProject
                {
                    FullPath = Path.Combine(TestRootPath, "A", "A.csproj"),
                    Name = "A",
                    ProjectGuid = new Guid("D744C26F-1CCB-456A-B490-CEB39334051B"),
                    ProjectTypeGuid = new Guid(projectTypeGuid),
                    Configurations = new[]
                    {
                        "Debug",
                        "Release",
                    },
                    Platforms = new[]
                    {
                        "AnyCPU",
                    },
                },
            });

            string path = Path.GetTempFileName();

            slnFile.Save(path, useFolders: false);

            string directoryName = new DirectoryInfo(TestRootPath).Name;

            File.ReadAllText(path).ShouldBe(
                $@"Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{{7E0F1516-6200-48BD-83FC-3EFA3AB4A574}}"") = ""C"", ""{directoryName}\C\C.csproj"", ""{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}""
EndProject
Project(""{{7E0F1516-6200-48BD-83FC-3EFA3AB4A574}}"") = ""A"", ""{directoryName}\A\A.csproj"", ""{{D744C26F-1CCB-456A-B490-CEB39334051B}}""
EndProject
Project(""{{7E0F1516-6200-48BD-83FC-3EFA3AB4A574}}"") = ""B"", ""{directoryName}\B\B.csproj"", ""{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{D744C26F-1CCB-456A-B490-CEB39334051B}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{D744C26F-1CCB-456A-B490-CEB39334051B}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{D744C26F-1CCB-456A-B490-CEB39334051B}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{D744C26F-1CCB-456A-B490-CEB39334051B}}.Release|Any CPU.Build.0 = Release|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{0CCA75AE-ED20-431E-8853-B9F54333E87A}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {{00CB211B-D6BE-49C6-AF57-2604ECCD9E72}}
	EndGlobalSection
EndGlobal
", StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void ProjectSolutionFolders()
        {
            string root = Path.GetTempPath();
            string projectName1 = Path.GetFileName(Path.GetTempFileName());
            string projectName2 = Path.GetFileName(Path.GetTempFileName());
            string projectName3 = Path.GetFileName(Path.GetTempFileName());
            string projectName4 = Path.GetFileName(Path.GetTempFileName());
            Project[] projects =
            {
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder1", "Project1", projectName1),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder2", "Project2", projectName2),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder3", "Project3", projectName3),
                },
                new Project
                {
                    FullPath = Path.Combine(root, "SubFolder4", "Project4", projectName4),
                },
            };

            projects[0].SetProperty(MSBuildPropertyNames.SlnGenSolutionFolder, "FolderA");
            projects[1].SetProperty(MSBuildPropertyNames.SlnGenSolutionFolder, "FolderB");
            projects[2].SetProperty(MSBuildPropertyNames.SlnGenSolutionFolder, "FolderB");

            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile();

            slnFile.AddProjects(projects, new Dictionary<string, Guid>(), projects[1].FullPath);
            slnFile.Save(solutionFilePath, useFolders: false);

            SolutionFile s = SolutionFile.Parse(solutionFilePath);

            ProjectInSolution project1 = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals(Path.GetFileNameWithoutExtension(projectName1))).Value;
            ProjectInSolution project2 = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals(Path.GetFileNameWithoutExtension(projectName2))).Value;
            ProjectInSolution project3 = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals(Path.GetFileNameWithoutExtension(projectName3))).Value;
            ProjectInSolution project4 = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals(Path.GetFileNameWithoutExtension(projectName4))).Value;
            ProjectInSolution folderA = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals("FolderA")).Value;
            ProjectInSolution folderB = s.ProjectsByGuid.FirstOrDefault(i => i.Value.ProjectName.Equals("FolderB")).Value;

            project1.ParentProjectGuid.ShouldBe(folderA.ProjectGuid);
            project2.ParentProjectGuid.ShouldBe(folderB.ProjectGuid);
            project3.ParentProjectGuid.ShouldBe(folderB.ProjectGuid);
            project4.ParentProjectGuid.ShouldBeNull();
            folderA.ProjectType.ShouldBe(SolutionProjectType.SolutionFolder);
            folderB.ProjectType.ShouldBe(SolutionProjectType.SolutionFolder);
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

            Dictionary<string, Guid> projects = new Dictionary<string, Guid>
            {
                [@"src\Microsoft.VisualStudio.SlnGen\Microsoft.VisualStudio.SlnGen.csproj"] = new Guid("C8D336E5-9E65-4D34-BA9A-DB58936947CF"),
                [@"test\Microsoft.VisualStudio.SlnGen.UnitTests\Microsoft.VisualStudio.SlnGen.UnitTests.csproj"] = new Guid("B55ACBF0-DC34-44BA-8535-8F81325B6D70"),
            };

            Dictionary<string, Guid> folders = new Dictionary<string, Guid>
            {
                [@"FolderA"] = new Guid("9C915FE4-72A5-4368-8979-32B3983E6041"),
                [@"FolderB"] = new Guid("D3A9F802-38CC-4F8D-8DE9-8DF9C8B7EADC"),
            };

            Dictionary<FileInfo, Guid> projectFiles = projects.ToDictionary(i => new FileInfo(Path.Combine(solutionFilePath.DirectoryName!, i.Key)), i => i.Value);

            foreach (KeyValuePair<FileInfo, Guid> item in projectFiles)
            {
                Directory.CreateDirectory(item.Key.DirectoryName!);

                File.WriteAllText(item.Key.FullName, @"<Project />");
            }

            File.WriteAllText(
                solutionFilePath.FullName,
                @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.29011.400
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Microsoft.VisualStudio.SlnGen"", ""src\Microsoft.VisualStudio.SlnGen\Microsoft.VisualStudio.SlnGen.csproj"", ""{C8D336E5-9E65-4D34-BA9A-DB58936947CF}""
EndProject
Project(""{9A19103F-16F7-4668-BE54-9A1E7A4F7556}"") = ""Microsoft.VisualStudio.SlnGen.UnitTests"", ""test\Microsoft.VisualStudio.SlnGen.UnitTests\Microsoft.VisualStudio.SlnGen.UnitTests.csproj"", ""{B55ACBF0-DC34-44BA-8535-8F81325B6D70}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""FolderA"", ""FolderA"", ""{9C915FE4-72A5-4368-8979-32B3983E6041}""
EndProject
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""FolderB"", ""FolderB"", ""{D3A9F802-38CC-4F8D-8DE9-8DF9C8B7EADC}""
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
		{B55ACBF0-DC34-44BA-8535-8F81325B6D70}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B55ACBF0-DC34-44BA-8535-8F81325B6D70}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B55ACBF0-DC34-44BA-8535-8F81325B6D70}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B55ACBF0-DC34-44BA-8535-8F81325B6D70}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(NestedProjects) = preSolution
		{C8D336E5-9E65-4D34-BA9A-DB58936947CF} = {9C915FE4-72A5-4368-8979-32B3983E6041}
		{B55ACBF0-DC34-44BA-8535-8F81325B6D70} = {D3A9F802-38CC-4F8D-8DE9-8DF9C8B7EADC}
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {CFFC4187-96EE-4465-B5B3-0BAFD3C14BB6}
	EndGlobalSection
EndGlobal
");
            SlnFile.TryParseExistingSolution(solutionFilePath.FullName, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath).ShouldBeTrue();

            solutionGuid.ShouldBe(Guid.Parse("CFFC4187-96EE-4465-B5B3-0BAFD3C14BB6"));

            projectGuidsByPath.ShouldBe(projects.Concat(folders));
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
        public void VisualStudioVersionIsWritten()
        {
            string solutionFilePath = GetTempFileName();

            SlnFile slnFile = new SlnFile
            {
                VisualStudioVersion = new Version(1, 2, 3, 4),
                SolutionGuid = new Guid("{6370DE27-36B7-44AE-B47A-1ECF4A6D740A}"),
            };

            slnFile.Save(solutionFilePath, useFolders: false);

            File.ReadAllText(solutionFilePath).ShouldBe(
                @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 1
VisualStudioVersion = 1.2.3.4
MinimumVisualStudioVersion = 10.0.40219.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {6370DE27-36B7-44AE-B47A-1ECF4A6D740A}
	EndGlobalSection
EndGlobal
",
                StringCompareShould.IgnoreLineEndings);
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