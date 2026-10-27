// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ProjectReferenceClosureTests : TestBase
    {
        /// <summary>
        /// Verifies that each entry project gets its own transitive dependencies and nothing else, with the entry project first.
        /// </summary>
        /// <param name="loaderName">The project loader to load the projects with.</param>
        [Theory]
        [InlineData("graph")]
#if NETFRAMEWORK
        [InlineData("legacy")]
#endif
        public void EachEntryProjectGetsOnlyItsOwnDependencies(string loaderName)
        {
            ProjectCreator l1 = CreateProject("L1");
            ProjectCreator l2 = CreateProject("L2");
            ProjectCreator c = CreateProject("C", l2);
            ProjectCreator b = CreateProject("B", l1);
            ProjectCreator a = CreateProject("A", l1, c);

            ProjectReferenceClosure closure = new ProjectReferenceClosure(LoadProjects(loaderName, a, b, c));

            closure.GetProjects(a).Select(i => i.FullPath).First().ShouldBe(Path.GetFullPath(a));
            GetPaths(closure.GetProjects(a)).ShouldBe(GetPaths(a, c, l1, l2), ignoreOrder: true);

            closure.GetProjects(b).Select(i => i.FullPath).First().ShouldBe(Path.GetFullPath(b));
            GetPaths(closure.GetProjects(b)).ShouldBe(GetPaths(b, l1), ignoreOrder: true);

            closure.GetProjects(c).Select(i => i.FullPath).First().ShouldBe(Path.GetFullPath(c));
            GetPaths(closure.GetProjects(c)).ShouldBe(GetPaths(c, l2), ignoreOrder: true);

            GetPaths(closure.GetProjects(l2)).ShouldBe(GetPaths(l2));
        }

        /// <summary>
        /// Verifies that members of a traversal project are part of its closure.
        /// </summary>
        /// <param name="loaderName">The project loader to load the projects with.</param>
        [Theory]
        [InlineData("graph")]
#if NETFRAMEWORK
        [InlineData("legacy")]
#endif
        public void TraversalProjectIncludesItsMembers(string loaderName)
        {
            ProjectCreator l1 = CreateProject("L1");
            ProjectCreator l2 = CreateProject("L2");
            ProjectCreator c = CreateProject("C", l2);
            ProjectCreator b = CreateProject("B", l1);
            ProjectCreator a = CreateProject("A", l1, c);

            // The static graph only follows ProjectReference items, while the legacy loader follows ProjectFile items of traversal projects
            ProjectCreator dirs = ProjectCreator
                .Create(Path.Combine(TestRootPath, "dirs", "dirs.proj"))
                .Property("IsTraversal", bool.TrueString);

            dirs = loaderName == "legacy"
                ? dirs.ItemInclude("ProjectFile", a.FullPath).ItemInclude("ProjectFile", b.FullPath)
                : dirs.ItemProjectReference(a).ItemProjectReference(b);

            dirs.Save();

            ProjectReferenceClosure closure = new ProjectReferenceClosure(LoadProjects(loaderName, dirs));

            GetPaths(closure.GetProjects(dirs)).ShouldBe(GetPaths(dirs, a, c, l1, l2, b), ignoreOrder: true);
        }

        /// <summary>
        /// Verifies that a project which was never loaded yields no projects.
        /// </summary>
        [Fact]
        public void UnknownEntryProjectReturnsEmptyList()
        {
            ProjectCreator a = CreateProject("A");
            ProjectCreator other = CreateProject("Other");

            ProjectReferenceClosure closure = new ProjectReferenceClosure(LoadProjects("graph", a));

            closure.GetProjects(other).ShouldBeEmpty();
        }

        /// <summary>
        /// Verifies that references that only exist in an inner build are part of the closure, while only the outer build is returned.
        /// </summary>
        [Fact]
        public void ReferencesOfInnerBuildsAreIncluded()
        {
            ProjectCreator onlyForNet46 = CreateProject("OnlyForNet46");

            ProjectCreator multiTargeted = ProjectCreator
                .Create(Path.Combine(TestRootPath, "Multi", "Multi.proj"))
                .ItemProjectReference(onlyForNet46, condition: "'$(TargetFramework)' == 'net46'")
                .Save();

            // An outer build has no TargetFramework, an inner build has one and the condition is true only there
            ProjectCollection projectCollection = new ProjectCollection();

            Project outer = new Project(multiTargeted.FullPath, new Dictionary<string, string>(), null, projectCollection);
            Project inner = new Project(multiTargeted.FullPath, new Dictionary<string, string> { ["TargetFramework"] = "net46" }, null, projectCollection);
            Project referenced = new Project(onlyForNet46.FullPath, new Dictionary<string, string>(), null, projectCollection);

            ProjectReferenceClosure closure = new ProjectReferenceClosure(new[] { outer, inner, referenced });

            IReadOnlyList<Project> projects = closure.GetProjects(multiTargeted);

            GetPaths(projects).ShouldBe(GetPaths(multiTargeted, onlyForNet46), ignoreOrder: true);
            projects.ShouldNotContain(inner);
        }

        /// <summary>
        /// Verifies that a project evaluated several times with different global properties does not break the closure.
        /// </summary>
        [Fact]
        public void ProjectEvaluatedWithDifferentGlobalPropertiesIsSupported()
        {
            ProjectCreator b = CreateProject("B");
            ProjectCreator a = CreateProject("A", b);

            ProjectCollection projectCollection = new ProjectCollection();

            Project[] loaded =
            {
                new Project(a.FullPath, new Dictionary<string, string> { ["Configuration"] = "Debug" }, null, projectCollection),
                new Project(a.FullPath, new Dictionary<string, string> { ["Configuration"] = "Release" }, null, projectCollection),
                new Project(b.FullPath, new Dictionary<string, string>(), null, projectCollection),
            };

            ProjectReferenceClosure closure = new ProjectReferenceClosure(loaded);

            GetPaths(closure.GetProjects(a)).Distinct().ShouldBe(GetPaths(a, b), ignoreOrder: true);
        }

        /// <summary>
        /// Gets the full paths of the specified projects.
        /// </summary>
        /// <param name="projects">The projects to get the paths of.</param>
        /// <returns>The full paths of the projects.</returns>
        private static IEnumerable<string> GetPaths(IEnumerable<Project> projects) => projects.Select(i => Path.GetFullPath(i.FullPath));

        /// <summary>
        /// Gets the full paths of the specified project files.
        /// </summary>
        /// <param name="projects">The project files to get the paths of.</param>
        /// <returns>The full paths of the project files.</returns>
        private static IEnumerable<string> GetPaths(params ProjectCreator[] projects) => projects.Select(i => Path.GetFullPath(i.FullPath));

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
        /// Loads the entry projects and their references with the specified loader.
        /// </summary>
        /// <param name="loaderName">Either "graph" or "legacy".</param>
        /// <param name="entryProjects">The entry projects.</param>
        /// <returns>All the projects that were loaded.</returns>
        private IReadOnlyCollection<Project> LoadProjects(string loaderName, params ProjectCreator[] entryProjects)
        {
            TestLogger logger = new TestLogger();

            ProjectCollection projectCollection = new ProjectCollection();

            IProjectLoader loader;

#if NETFRAMEWORK
            loader = loaderName == "legacy" ? new LegacyProjectLoader(logger) : new ProjectGraphProjectLoader(logger);
#else
            loader = new ProjectGraphProjectLoader(logger);
#endif

            loader.LoadProjects(entryProjects.Select(i => i.FullPath), projectCollection, new ProgramArguments().GetGlobalProperties());

            logger.ErrorMessages.ShouldBeEmpty();

            return projectCollection.LoadedProjects.ToList();
        }
    }
}
