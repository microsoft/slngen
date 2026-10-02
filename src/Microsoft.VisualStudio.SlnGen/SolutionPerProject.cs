// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Generates one solution per entry project from a project graph that is already loaded.
    /// </summary>
    public sealed class SolutionPerProject
    {
        private SolutionPerProject()
        {
        }

        /// <summary>
        /// Generates one solution per entry project, each containing only that project and its transitive references.
        /// Nothing is written unless every solution can be generated without a path conflict.
        /// </summary>
        /// <param name="arguments">The <see cref="ProgramArguments" /> to use.</param>
        /// <param name="loadedProjects">All the projects that were loaded for the entry projects.</param>
        /// <param name="projectEntryPaths">The paths of the entry projects.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <returns>Zero if every solution was generated, otherwise non-zero.</returns>
        public static int GenerateSolutions(ProgramArguments arguments, IEnumerable<Project> loadedProjects, IReadOnlyList<string> projectEntryPaths, ISlnGenLogger logger)
        {
            // The same project can be listed more than once (for example via overlapping wildcards)
            List<string> entryPaths = projectEntryPaths
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ProjectReferenceClosure closure = new ProjectReferenceClosure(loadedProjects);

            List<(string EntryPath, string SolutionPath, IReadOnlyList<Project> Projects)> solutions = new ();

            // Work out every solution's content and path first so problems are found before anything is written
            foreach (string entryPath in entryPaths)
            {
                IReadOnlyList<Project> projects = closure.GetProjects(entryPath);

                if (projects.Count == 0)
                {
                    logger.LogError($"Project \"{entryPath}\" was not loaded");

                    continue;
                }

                solutions.Add((entryPath, Path.GetFullPath(SlnFile.GetSolutionFileFullPath(arguments, projects[0])), projects));
            }

            // Two entry projects writing to the same file would silently overwrite each other
            foreach (IGrouping<string, (string EntryPath, string SolutionPath, IReadOnlyList<Project> Projects)> collision in solutions
                .GroupBy(i => i.SolutionPath, StringComparer.OrdinalIgnoreCase)
                .Where(i => i.Count() > 1))
            {
                logger.LogError($"Projects {string.Join(", ", collision.Select(i => $"\"{i.EntryPath}\""))} would all generate the solution \"{collision.Key}\".  Use different project names or solution directories.");
            }

            if (logger.HasLoggedErrors)
            {
                return 1;
            }

            foreach ((string _, string _, IReadOnlyList<Project> projects) in solutions)
            {
                SlnFile.GenerateSolutionFile(arguments, projects, logger);
            }

            logger.LogMessageHigh($"Generated {solutions.Count:N0} solution(s)");

            return logger.HasLoggedErrors ? 1 : 0;
        }
    }
}
