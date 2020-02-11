// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents a class that loads projects via the Static Graph API.
    /// </summary>
    internal class ProjectGraphProjectLoader : IProjectLoader
    {
        private static readonly ProjectLoadSettings DefaultProjectLoadSettings =
            ProjectLoadSettings.IgnoreEmptyImports
            | ProjectLoadSettings.IgnoreInvalidImports
            | ProjectLoadSettings.IgnoreMissingImports
            | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;

        private static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

        private readonly string _msbuildExePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectGraphProjectLoader"/> class.
        /// </summary>
        /// <param name="msbuildExePath">The full path to MSBuild.exe.</param>
        public ProjectGraphProjectLoader(string msbuildExePath)
        {
            _msbuildExePath = msbuildExePath;
        }

        /// <inheritdoc />
        public void LoadProjects(IEnumerable<string> projectPaths, ProjectCollection projectCollection, IDictionary<string, string> globalProperties)
        {
            ICollection<ProjectGraphEntryPoint> entryProjects = projectPaths.Select(i => new ProjectGraphEntryPoint(i, globalProperties)).ToList();

            ProjectGraph unused = new ProjectGraph(entryProjects, projectCollection, CreateProjectInstance);
        }

        private ProjectInstance CreateProjectInstance(string projectFullPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            return Project.FromFile(
                    projectFullPath,
                    new ProjectOptions
                    {
                        EvaluationContext = SharedEvaluationContext,
                        GlobalProperties = globalProperties,
                        LoadSettings = DefaultProjectLoadSettings,
                        ProjectCollection = projectCollection,
                    })
                .CreateProjectInstance(
                    ProjectInstanceSettings.ImmutableWithFastItemLookup,
                    SharedEvaluationContext);
        }
    }
}