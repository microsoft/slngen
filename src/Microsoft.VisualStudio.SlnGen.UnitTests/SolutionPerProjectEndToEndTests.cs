// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    /// <summary>
    /// Runs the slngen entry point in-process with --solutionPerProject to verify that <see cref="Program" /> wires the mode up.
    /// The generation rules themselves are covered by <see cref="SolutionPerProjectTests" />.
    /// </summary>
    public sealed class SolutionPerProjectEndToEndTests : TestBase
    {
        /// <summary>
        /// Verifies that the graph is loaded once for all entry projects and that each solution equals the result of
        /// running slngen for that project alone.
        /// </summary>
        [Fact]
        public void SolutionsMatchSingleRunsAndGraphIsLoadedOnce()
        {
            ProjectCreator l1 = CreateProject("L1");
            ProjectCreator l2 = CreateProject("L2");
            ProjectCreator c = CreateProject("C", l2);
            ProjectCreator b = CreateProject("B", l1);
            ProjectCreator a = CreateProject("A", l1, c);

            (int exitCode, string output) = RunSlnGen("--solutionPerProject", "--launch:false", a, b, c);

            exitCode.ShouldBe(0, output);

            // One load of the combined graph is what makes the mode worthwhile
            CountOccurrences(output, "Loading project references...").ShouldBe(1, output);

            foreach (ProjectCreator entry in new[] { a, b, c })
            {
                string singleSolution = Path.Combine(TestRootPath, "single", Path.GetFileName(Path.ChangeExtension(entry, ".sln")));

                (int singleExitCode, string singleOutput) = RunSlnGen("--launch:false", $"--solutionfile:{singleSolution}", entry);

                singleExitCode.ShouldBe(0, singleOutput);

                GetSolutionProjects(Path.ChangeExtension(entry, ".sln")).ShouldBe(GetSolutionProjects(singleSolution), ignoreOrder: true);
            }
        }

        /// <summary>
        /// Counts how often a value appears in a string.
        /// </summary>
        /// <param name="text">The text to search.</param>
        /// <param name="value">The value to count.</param>
        /// <returns>The number of occurrences.</returns>
        private static int CountOccurrences(string text, string value)
        {
            int count = 0;

            for (int index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

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
            ProjectCreator project = ProjectCreator.Create(Path.Combine(TestRootPath, name, $"{name}.csproj"));

            foreach (ProjectCreator reference in references)
            {
                project.ItemProjectReference(reference);
            }

            return project.Save();
        }

        /// <summary>
        /// Runs the slngen entry point in-process with a test console.
        /// </summary>
        /// <param name="arguments">Command-line switches followed by the projects to generate solutions for.</param>
        /// <returns>The exit code and the combined standard output and error.</returns>
        private (int ExitCode, string Output) RunSlnGen(params object[] arguments)
        {
            // Main normally does this; the rest of Program.Execute depends on it
            Program.CurrentDevelopmentEnvironment ??= DevelopmentEnvironment.LoadCurrentDevelopmentEnvironment(SystemEnvironmentProvider.Instance);

            string[] args = new[] { "--nologo" }.Concat(arguments.Select(i => i is ProjectCreator project ? project.FullPath : (string)i)).ToArray();

            TestConsole console = new TestConsole();

            // Passing the real implementation also resets the static hook that other tests replace
            int exitCode = Program.Execute(args, console, Program.Execute);

            return (exitCode, console.AllOutput);
        }
    }
}
