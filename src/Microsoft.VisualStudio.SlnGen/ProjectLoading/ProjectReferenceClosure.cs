// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen.ProjectLoading
{
    /// <summary>
    /// Computes the transitive set of projects reachable from an entry project, using projects that are already evaluated.
    /// This lets one loaded graph serve several solutions without evaluating any project again.
    /// </summary>
    internal sealed class ProjectReferenceClosure
    {
        /// <summary>
        /// Outer-build projects (no TargetFramework global property) by full path.  A path can have several instances
        /// when it was evaluated with different global properties.
        /// </summary>
        private readonly Dictionary<string, List<Project>> _outerProjects = new (StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The full paths of the projects each project depends on, merged across all of its evaluations.
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _references = new (StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectReferenceClosure"/> class.
        /// </summary>
        /// <param name="loadedProjects">The evaluated projects to build the dependency index from.</param>
        public ProjectReferenceClosure(IEnumerable<Project> loadedProjects)
        {
            foreach (Project project in loadedProjects)
            {
                string fullPath = Path.GetFullPath(project.FullPath);

                // Only outer builds become solution projects, matching what a single-solution run passes on
                if (!project.GlobalProperties.ContainsKey("TargetFramework"))
                {
                    if (!_outerProjects.TryGetValue(fullPath, out List<Project> instances))
                    {
                        instances = new List<Project>();
                        _outerProjects[fullPath] = instances;
                    }

                    instances.Add(project);
                }

                // Inner builds are indexed too, because references can exist only under a TargetFramework condition
                if (!_references.TryGetValue(fullPath, out HashSet<string> references))
                {
                    references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _references[fullPath] = references;
                }

                foreach (string referencePath in GetReferencePaths(project))
                {
                    if (!string.Equals(referencePath, fullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        references.Add(referencePath);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the projects that belong in the solution of the specified entry project.
        /// </summary>
        /// <param name="entryProjectPath">The path to the entry project.</param>
        /// <returns>The entry project followed by its transitive dependencies, or an empty list if the entry project was not loaded.</returns>
        public IReadOnlyList<Project> GetProjects(string entryProjectPath)
        {
            string entryPath = Path.GetFullPath(entryProjectPath);

            if (!_outerProjects.ContainsKey(entryPath))
            {
                return Array.Empty<Project>();
            }

            // Breadth-first walk keeps the entry project first, which GenerateSolutionFile treats as the main project
            List<string> orderedPaths = new List<string> { entryPath };
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entryPath };

            for (int i = 0; i < orderedPaths.Count; i++)
            {
                if (!_references.TryGetValue(orderedPaths[i], out HashSet<string> references))
                {
                    continue;
                }

                foreach (string referencePath in references.OrderBy(j => j, StringComparer.OrdinalIgnoreCase))
                {
                    if (visited.Add(referencePath))
                    {
                        orderedPaths.Add(referencePath);
                    }
                }
            }

            // Paths that were never loaded (for example missing references already reported by the loader) are dropped
            return orderedPaths
                .Where(_outerProjects.ContainsKey)
                .SelectMany(i => _outerProjects[i])
                .ToList();
        }

        /// <summary>
        /// Gets the full paths of everything a project depends on: project references, traversal members and shared project items.
        /// </summary>
        /// <param name="project">The evaluated project.</param>
        /// <returns>The full paths of the dependencies.</returns>
        private static IEnumerable<string> GetReferencePaths(Project project)
        {
            IEnumerable<ProjectItem> items = project.GetItems(MSBuildItemNames.ProjectReference);

            // Traversal projects list their members as ProjectFile items
            if (project.IsPropertyValueTrue(MSBuildPropertyNames.IsTraversal) || project.IsPropertyValueTrue(MSBuildPropertyNames.IsTraversalProject))
            {
                items = items.Concat(project.GetItems(MSBuildItemNames.ProjectFile));
            }

            foreach (ProjectItem item in items)
            {
                yield return Path.IsPathRooted(item.EvaluatedInclude)
                    ? Path.GetFullPath(item.EvaluatedInclude)
                    : Path.GetFullPath(Path.Combine(project.DirectoryPath, item.EvaluatedInclude));
            }

            // Shared projects are only discovered through the .projitems / .vcxitems files a project imports
            if (!string.Equals(project.GetPropertyValue("HasSharedItems"), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            foreach (ResolvedImport import in project.Imports)
            {
                string importPath = import.ImportedProject.FullPath;

                if (importPath.EndsWith(ProjectFileExtensions.ProjItems, StringComparison.Ordinal))
                {
                    yield return Path.GetFullPath(Path.ChangeExtension(importPath, ProjectFileExtensions.Shproj));
                }
                else if (importPath.EndsWith(ProjectFileExtensions.VcxItems, StringComparison.Ordinal))
                {
                    yield return Path.GetFullPath(importPath);
                }
            }
        }
    }
}
