// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

#if !NET461
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using System.Collections.Generic;
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
            ICollection<ProjectGraphEntryPoint> entryProjects = projectPaths.Select(i => new ProjectGraphEntryPoint(i, globalProperties)).ToList();

            ProjectGraph unused = new ProjectGraph(entryProjects, projectCollection, CreateProjectInstance);
        }

        private ProjectInstance CreateProjectInstance(string projectFullPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            ProjectInstance projectInstance = Project.FromFile(
                    projectFullPath,
                    new ProjectOptions
                    {
                        EvaluationContext = ProjectLoader.SharedEvaluationContext,
                        GlobalProperties = globalProperties,
                        LoadSettings = DefaultProjectLoadSettings,
                        ProjectCollection = projectCollection,
                    })
                .CreateProjectInstance(
                    ProjectInstanceSettings.None,
                    ProjectLoader.SharedEvaluationContext);

            ProjectLoader.LogProjectStartedEvent(_logger, projectInstance);

            return projectInstance;
        }
    }
}
#endif