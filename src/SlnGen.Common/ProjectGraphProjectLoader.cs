// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SlnGen.Common
{
    internal class ProjectGraphProjectLoader : IMSBuildProjectLoader
    {
        private static readonly ProjectLoadSettings DefaultProjectLoadSettings =
            ProjectLoadSettings.IgnoreEmptyImports
            | ProjectLoadSettings.IgnoreInvalidImports
            | ProjectLoadSettings.IgnoreMissingImports
            | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;

        private static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

        private readonly ISlnGenLogger _logger;
        private readonly string _msbuildExePath;

        public ProjectGraphProjectLoader(ISlnGenLogger logger, string msbuildExePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _msbuildExePath = msbuildExePath;
        }

        public void LoadProjects(ProjectCollection projectCollection, IDictionary<string, string> globalProperties, IEnumerable<string> projectPaths)
        {
            using (new MSBuildFeatureFlags
            {
                MSBuildCacheFileEnumerations = true,
                MSBuildLoadAllFilesAsReadonly = true,
                MSBuildSkipEagerWildCardEvaluationRegexes = true,
                MSBuildUseSimpleProjectRootElementCacheConcurrency = true,
#if NETFRAMEWORK
                MSBUILD_EXE_PATH = _msbuildExePath,
#endif
            })
            {
                ICollection<ProjectGraphEntryPoint> entryProjects = projectPaths.Select(i => new ProjectGraphEntryPoint(i, globalProperties)).ToList();

                _ = new ProjectGraph(entryProjects, projectCollection, CreateProjectInstance);
            }
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