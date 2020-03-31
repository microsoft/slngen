// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents a factory class for creating an instance of a class that implements <see cref="IProjectLoader" />.
    /// </summary>
    internal static class ProjectLoaderFactory
    {
        /// <summary>
        /// Gets a shared <see cref="EvaluationContext" /> object to use.
        /// </summary>
        public static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

        /// <summary>
        /// Creates an appropriate instance of a class that implements <see cref="IProjectLoader" />.
        /// </summary>
        /// <param name="msbuildExePath">The full path to MSBuild.exe.</param>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> object to use for logging.</param>
        /// <returns>An <see cref="IProjectLoader" /> object that can be used to load MSBuild projects.</returns>
        public static IProjectLoader Create(FileInfo msbuildExePath, ISlnGenLogger logger)
        {
#if !NET46
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(msbuildExePath.FullName);

            // MSBuild 16.4 and above use the Static Graph API
            if (fileVersionInfo.FileMajorPart >= 16 && fileVersionInfo.FileMinorPart >= 4)
            {
                return new ProjectGraphProjectLoader(logger);
            }
#endif

            return new LegacyProjectLoader(logger);
        }

        /// <summary>
        /// Logs a <see cref="ProjectStartedEventArgs" /> object for the specified project.
        /// </summary>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> to use.</param>
        /// <param name="projectInstance">The <see cref="ProjectInstance" /> of the project.</param>
        internal static void LogProjectStartedEvent(ISlnGenLogger logger, ProjectInstance projectInstance)
        {
            if (!logger.IsDiagnostic)
            {
                return;
            }

            int projectId = logger.NextProjectId;

            logger.LogEvent(new ProjectStartedEventArgs(
                projectId: projectId,
                message: $"Project \"{projectInstance.FullPath}\"",
                helpKeyword: null,
                projectFile: projectInstance.FullPath,
                targetNames: null,
                properties: projectInstance.Properties.Select(i => new DictionaryEntry(i.Name, i.EvaluatedValue)),
                items: projectInstance.Items.Select(i => new DictionaryEntry(i.ItemType, new ProjectItemWrapper(i))),
                parentBuildEventContext: BuildEventContext.Invalid,
                globalProperties: projectInstance.GlobalProperties,
                toolsVersion: null)
            {
                BuildEventContext = new BuildEventContext(BuildEventContext.InvalidSubmissionId, BuildEventContext.InvalidNodeId, projectInstance.EvaluationId, projectId, projectId, BuildEventContext.InvalidTargetId, BuildEventContext.InvalidTaskId),
            });
        }
    }
}