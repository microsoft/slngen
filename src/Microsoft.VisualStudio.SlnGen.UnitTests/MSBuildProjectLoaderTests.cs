// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class MSBuildProjectLoaderTests : TestBase
    {
        [Fact]
        public void ArgumentNullException_Logger()
        {
            ArgumentNullException exception = Should.Throw<ArgumentNullException>(() =>
            {
                LegacyProjectLoader unused = new LegacyProjectLoader(logger: null);
            });

            exception.ParamName.ShouldBe("logger");
        }

        [Fact]
        public void BuildFailsIfError()
        {
            ProjectCreator dirsProj = ProjectCreator
                .Create(GetTempFileName())
                .Property("IsTraversal", bool.TrueString)
                .ItemInclude("ProjectFile", "does not exist")
                .Save();

            TestLogger logger = new TestLogger();

            LegacyProjectLoader loader = new LegacyProjectLoader(logger);

            ProjectCollection projectCollection = new ProjectCollection();

            loader.LoadProjects(new[] { dirsProj.FullPath }, projectCollection, null);

            logger.ErrorMessages.ShouldHaveSingleItem().ShouldStartWith("The project file could not be loaded. Could not find file ");
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

            TestLogger logger = new TestLogger();

            LegacyProjectLoader loader = new LegacyProjectLoader(logger);

            ProjectCollection projectCollection = new ProjectCollection();

            loader.LoadProjects(new[] { projectA.FullPath }, projectCollection, expectedGlobalProperties);

            Project project = projectCollection.LoadedProjects.ShouldHaveSingleItem();

            project.GlobalProperties.ShouldBe(expectedGlobalProperties);
        }

        [Fact]
        public void InvalidProjectsLogGoodInfo()
        {
            string projectA = GetTempFileName();

            File.WriteAllText(projectA, "Invalid XML");

            ProjectCreator dirsProj = ProjectCreator
                .Create(GetTempFileName())
                .Property("IsTraversal", bool.TrueString)
                .ItemInclude("ProjectFile", projectA)
                .Save();

            TestLogger logger = new TestLogger();

            ProjectCollection projectCollection = new ProjectCollection();

            LegacyProjectLoader loader = new LegacyProjectLoader(logger);

            loader.LoadProjects(new[] { dirsProj.FullPath }, projectCollection, null);

            BuildErrorEventArgs errorEventArgs = logger.Errors.ShouldHaveSingleItem();

            errorEventArgs.Message.ShouldStartWith("The project file could not be loaded. Data at the root level is invalid. Line 1, position 1.");
            errorEventArgs.Code.ShouldBe("MSB4025");
            errorEventArgs.ColumnNumber.ShouldBe(1);
            errorEventArgs.LineNumber.ShouldBe(1);
            errorEventArgs.File.ShouldBe(projectA);
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

            TestLogger logger = new TestLogger();

            ProjectCollection projectCollection = new ProjectCollection();

            LegacyProjectLoader loader = new LegacyProjectLoader(logger);

            loader.LoadProjects(new[] { projectA.FullPath }, projectCollection, null);

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
                .Property("IsTraversal", bool.TrueString)
                .ItemInclude("ProjectFile", projectA.FullPath)
                .Save();

            TestLogger logger = new TestLogger();

            ProjectCollection projectCollection = new ProjectCollection();

            LegacyProjectLoader loader = new LegacyProjectLoader(logger);

            loader.LoadProjects(new[] { dirsProj.FullPath }, projectCollection, null);

            projectCollection.LoadedProjects.Select(i => i.FullPath).ShouldBe(
                new[] { dirsProj.FullPath, projectA.FullPath, projectB.FullPath },
                ignoreOrder: true);
        }
    }
}