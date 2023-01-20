// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

#if !NET461
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents a class that loads projects via the Static Graph API.
    /// </summary>
    internal class ProjectGraphProjectLoader : IProjectLoader
    {
        private static readonly ProjectLoadSettings DefaultProjectLoadSettings = ProjectLoadSettings.IgnoreEmptyImports
                                                                                 | ProjectLoadSettings.IgnoreInvalidImports
                                                                                 | ProjectLoadSettings.IgnoreMissingImports
                                                                                 | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;

        private static readonly ConcurrentDictionary<string, byte> LoadedProjects = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private readonly ISlnGenLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectGraphProjectLoader"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> to use for logging.</param>
        public ProjectGraphProjectLoader(ISlnGenLogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void LoadProjects(IEnumerable<string> projectPaths, ProjectCollection projectCollection, IDictionary<string, string> globalProperties)
        {
            ProjectGraph projectGraph = new ProjectGraph(
                projectPaths.Select(i => new ProjectGraphEntryPoint(i, globalProperties)),
                projectCollection,
                CreateProjectInstance);

            foreach (ProjectInstance projectInstance in projectGraph.ProjectNodes.Select(i => i.ProjectInstance))
            {
                if (!string.Equals(projectInstance.GetPropertyValue("HasSharedItems"), bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string item in projectInstance.ImportPaths.Where(i => i.EndsWith(ProjectFileExtensions.ProjItems, StringComparison.Ordinal)))
                {
                    FileInfo projectPath = new FileInfo(Path.ChangeExtension(item, ProjectFileExtensions.Shproj));

                    if (projectPath.Exists && !LoadedProjects.ContainsKey(projectPath.FullName))
                    {
                        _ = CreateProject(projectPath.FullName, globalProperties, projectCollection);
                    }
                }
            }
        }

        private ProjectInstance CreateProjectInstance(string projectFullPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            ProjectInstance projectInstance = CreateProject(
                projectFullPath,
                globalProperties,
                projectCollection)
                .CreateProjectInstance(
                    ProjectInstanceSettings.None,
                    ProjectLoader.SharedEvaluationContext);

            ProjectLoader.LogProjectStartedEvent(_logger, projectInstance);

            return projectInstance;
        }

        private Project CreateProject(string projectFullPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            Project project = Project.FromFile(
                projectFullPath,
                new ProjectOptions
                {
                    EvaluationContext = ProjectLoader.SharedEvaluationContext,
                    GlobalProperties = globalProperties,
                    LoadSettings = DefaultProjectLoadSettings,
                    ProjectCollection = projectCollection,
                });

            LoadedProjects.TryAdd(projectFullPath, 0);

            return project;
        }
    }
}
#endif