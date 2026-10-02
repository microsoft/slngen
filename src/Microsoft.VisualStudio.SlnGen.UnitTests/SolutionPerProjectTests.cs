// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    /// <summary>
    /// Tests for generating one solution per entry project.
    /// </summary>
    public sealed class SolutionPerProjectTests : TestBase
    {
        /// <summary>
        /// Verifies that each entry project gets exactly its own dependencies and that only entry projects get a solution.
        /// </summary>
        [Fact]
        public void EachEntryProjectGetsItsOwnSolution()
        {
            ProjectCreator l1 = CreateProject("L1");
            ProjectCreator l2 = CreateProject("L2");
            ProjectCreator c = CreateProject("C", l2);
            ProjectCreator b = CreateProject("B", l1);
            ProjectCreator a = CreateProject("A", l1, c);

            TestLogger logger = GenerateSolutions(new ProgramArguments(), out int exitCode, a.FullPath, b.FullPath, c.FullPath);

            exitCode.ShouldBe(0, string.Join(Environment.NewLine, logger.ErrorMessages));
            logger.HighImportanceMessages.ShouldContain("Generated 3 solution(s)");

            GetSolutionProjects(Path.ChangeExtension(a, ".sln")).ShouldBe(GetPaths(a, c, l1, l2), ignoreOrder: true);
            GetSolutionProjects(Path.ChangeExtension(b, ".sln")).ShouldBe(GetPaths(b, l1), ignoreOrder: true);
            GetSolutionProjects(Path.ChangeExtension(c, ".sln")).ShouldBe(GetPaths(c, l2), ignoreOrder: true);

            File.Exists(Path.ChangeExtension(l1, ".sln")).ShouldBeFalse();
            File.Exists(Path.ChangeExtension(l2, ".sln")).ShouldBeFalse();
        }

        /// <summary>
        /// Verifies that an entry project listed more than once, with different spellings of its path, is generated once.
        /// </summary>
        [Fact]
        public void DuplicateEntryPathsAreGeneratedOnce()
        {
            ProjectCreator a = CreateProject("A");

            string respelled = Path.Combine(Path.GetDirectoryName(a.FullPath), ".", Path.GetFileName(a.FullPath));

            // Load by the canonical path only; the entry list is what carries the duplicate
            ProjectCollection projectCollection = LoadProjects(a.FullPath);

            TestLogger logger = new TestLogger();

            int exitCode = SolutionPerProject.GenerateSolutions(new ProgramArguments(), projectCollection.LoadedProjects, new[] { a.FullPath, respelled }, logger);

            exitCode.ShouldBe(0, string.Join(Environment.NewLine, logger.ErrorMessages));
            logger.HighImportanceMessages.ShouldContain("Generated 1 solution(s)");
        }

        /// <summary>
        /// Verifies that an entry project that was never loaded is reported and nothing is written.
        /// </summary>
        [Fact]
        public void EntryProjectThatWasNotLoadedIsReported()
        {
            ProjectCreator a = CreateProject("A");
            ProjectCreator notLoaded = CreateProject("NotLoaded");

            ProjectCollection projectCollection = LoadProjects(a.FullPath);

            TestLogger logger = new TestLogger();

            int exitCode = SolutionPerProject.GenerateSolutions(new ProgramArguments(), projectCollection.LoadedProjects, new[] { a.FullPath, notLoaded.FullPath }, logger);

            exitCode.ShouldNotBe(0);
            logger.ErrorMessages.ShouldHaveSingleItem().ShouldContain("was not loaded");
            Directory.GetFiles(TestRootPath, "*.sln", SearchOption.AllDirectories).ShouldBeEmpty();
        }

        /// <summary>
        /// Verifies that two projects that would write the same solution file are reported and nothing is written.
        /// </summary>
        [Fact]
        public void CollidingSolutionPathsAreRejected()
        {
            ProjectCreator first = ProjectCreator.Create(Path.Combine(TestRootPath, "d1", "X.proj")).Save();
            ProjectCreator second = ProjectCreator.Create(Path.Combine(TestRootPath, "d2", "X.proj")).Save();
            string sharedDirectory = Path.Combine(TestRootPath, "shared");

            ProgramArguments arguments = new ProgramArguments { SolutionDirectoryFullPath = new[] { sharedDirectory } };

            TestLogger logger = GenerateSolutions(arguments, out int exitCode, first.FullPath, second.FullPath);

            exitCode.ShouldNotBe(0);

            string error = logger.ErrorMessages.ShouldHaveSingleItem();

            error.ShouldContain("would all generate the solution");
            error.ShouldContain(first.FullPath);
            error.ShouldContain(second.FullPath);
            Directory.Exists(sharedDirectory).ShouldBeFalse();
            Directory.GetFiles(TestRootPath, "*.sln", SearchOption.AllDirectories).ShouldBeEmpty();
        }

        /// <summary>
        /// Gets the full paths of the specified project files.
        /// </summary>
        /// <param name="projects">The project files.</param>
        /// <returns>The full paths.</returns>
        private static string[] GetPaths(params ProjectCreator[] projects) => projects.Select(i => Path.GetFullPath(i.FullPath)).ToArray();

        /// <summary>
        /// Gets the full paths of the projects in a solution file.
        /// </summary>
        /// <param name="solutionPath">The path to the solution file.</param>
        /// <returns>The full paths of the projects in the solution.</returns>
        private static string[] GetSolutionProjects(string solutionPath)
        {
            File.Exists(solutionPath).ShouldBeTrue($"Solution file \"{solutionPath}\" was not generated");

            return SolutionFile.Parse(solutionPath).ProjectsInOrder.Select(i => Path.GetFullPath(i.AbsolutePath)).ToArray();
        }

        /// <summary>
        /// Creates a project in its own directory that references the specified projects.
        /// </summary>
        /// <param name="name">The name of the project.</param>
        /// <param name="references">The projects it references.</param>
        /// <returns>The saved project.</returns>
        private ProjectCreator CreateProject(string name, params ProjectCreator[] references)
        {
            ProjectCreator project = ProjectCreator.Create(Path.Combine(TestRootPath, name, $"{name}.proj"));

            foreach (ProjectCreator reference in references)
            {
                project.ItemProjectReference(reference);
            }

            return project.Save();
        }

        /// <summary>
        /// Loads the entry projects and their references.
        /// </summary>
        /// <param name="entryPaths">The paths of the entry projects.</param>
        /// <returns>The <see cref="ProjectCollection" /> containing everything that was loaded.</returns>
        private ProjectCollection LoadProjects(params string[] entryPaths)
        {
            TestLogger loadLogger = new TestLogger();

            ProjectCollection projectCollection = new ProjectCollection();

            new ProjectGraphProjectLoader(loadLogger).LoadProjects(entryPaths, projectCollection, new ProgramArguments().GetGlobalProperties());

            loadLogger.ErrorMessages.ShouldBeEmpty();

            return projectCollection;
        }

        /// <summary>
        /// Loads the entry projects and generates a solution for each of them.
        /// </summary>
        /// <param name="arguments">The <see cref="ProgramArguments" /> to use.</param>
        /// <param name="exitCode">Receives the result of generating the solutions.</param>
        /// <param name="entryPaths">The paths of the entry projects.</param>
        /// <returns>The logger that was used for generating.</returns>
        private TestLogger GenerateSolutions(ProgramArguments arguments, out int exitCode, params string[] entryPaths)
        {
            ProjectCollection projectCollection = LoadProjects(entryPaths);

            TestLogger logger = new TestLogger();

            exitCode = SolutionPerProject.GenerateSolutions(arguments, projectCollection.LoadedProjects, entryPaths, logger);

            return logger;
        }
    }
}
