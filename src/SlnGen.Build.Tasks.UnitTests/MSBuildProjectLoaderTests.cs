// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public class MSBuildProjectLoaderTests : TestBase
    {
        [Fact]
        public void ArgumentNullException_BuildEngine()
        {
            ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            {
                _ = new MSBuildProjectLoader(globalProperties: null, toolsVersion: null, buildEngine: null);
            });

            exception.ParamName.ShouldBe("buildEngine");
        }

        [Fact]
        public void BuildFailsIfError()
        {
            ProjectCreator dirsProj = ProjectCreator
                .Create(GetTempFileName())
                .Property("IsTraversal", "true")
                .ItemInclude("ProjectFile", "does not exist")
                .Save();

            BuildEngine buildEngine = BuildEngine.Create();

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, ProjectCollection.GlobalProjectCollection.DefaultToolsVersion, buildEngine);

            loader.LoadProjectsAndReferences(new[] { dirsProj.FullPath });

            buildEngine.Errors.ShouldHaveSingleItem().ShouldStartWith("The project file could not be loaded. Could not find file ");
        }

        [Fact]
        public void GlobalPropertiesSetCorrectly()
        {
            Dictionary<string, string> expectedGlobalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Property1"] = "1A836FEB3ABA43B183034DFDD5C4E375",
                ["Property2"] = "CEEC5C9FF0F344DAA32A0F545460EB2C",
            };

            ProjectCreator projectA = ProjectCreator
                .Create(GetTempFileName())
                .Save();

            BuildEngine buildEngine = BuildEngine.Create();

            MSBuildProjectLoader loader = new MSBuildProjectLoader(expectedGlobalProperties, ProjectCollection.GlobalProjectCollection.DefaultToolsVersion, buildEngine);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] { projectA.FullPath });

            projectCollection.GlobalProperties.ShouldBe(expectedGlobalProperties);
        }

        [Fact]
        public void InvalidProjectsLogGoodInfo()
        {
            ProjectCreator projectA = ProjectCreator
                .Create(GetTempFileName())
                .Import(@"$(Foo)\foo.props")
                .Save();

            ProjectCreator dirsProj = ProjectCreator
                .Create(GetTempFileName())
                .Property("IsTraversal", "true")
                .ItemInclude("ProjectFile", projectA.FullPath)
                .Save();

            BuildEngine buildEngine = BuildEngine.Create();

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, ProjectCollection.GlobalProjectCollection.DefaultToolsVersion, buildEngine);

            loader.LoadProjectsAndReferences(new[] { dirsProj.FullPath });

            BuildErrorEventArgs errorEventArgs = buildEngine.ErrorEvents.ShouldHaveSingleItem();

            errorEventArgs.Code.ShouldBe("MSB4019");
            errorEventArgs.ColumnNumber.ShouldBe(3);
            errorEventArgs.HelpKeyword.ShouldBe("MSBuild.ImportedProjectNotFound");
            errorEventArgs.LineNumber.ShouldBe(3);
            errorEventArgs.File.ShouldBe(projectA.FullPath);
        }

        [Fact]
        public void ProjectReferencesWork()
        {
            ProjectCreator projectB = ProjectCreator
                .Create(GetTempFileName())
                .Save();

            ProjectCreator projectA = ProjectCreator
                .Create(GetTempFileName())
                .ItemProjectReference(projectB)
                .Save();

            BuildEngine buildEngine = BuildEngine.Create();

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, ProjectCollection.GlobalProjectCollection.DefaultToolsVersion, buildEngine);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] { projectA.FullPath });

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(new[] { projectA.FullPath, projectB.FullPath });
        }

        [Fact]
        public void TraversalReferencesWork()
        {
            ProjectCreator projectB = ProjectCreator
                .Create(GetTempFileName())
                .Save();

            ProjectCreator projectA = ProjectCreator
                .Create(GetTempFileName())
                .ItemProjectReference(projectB)
                .Save();

            ProjectCreator dirsProj = ProjectCreator
                .Create(GetTempFileName())
                .Property("IsTraversal", "true")
                .ItemInclude("ProjectFile", projectA.FullPath)
                .Save();

            BuildEngine buildEngine = BuildEngine.Create();

            MSBuildProjectLoader loader = new MSBuildProjectLoader(null, ProjectCollection.GlobalProjectCollection.DefaultToolsVersion, buildEngine);

            ProjectCollection projectCollection = loader.LoadProjectsAndReferences(new[] { dirsProj.FullPath });

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(
                new[] { dirsProj.FullPath, projectA.FullPath, projectB.FullPath },
                ignoreOrder: true);
        }
    }
}