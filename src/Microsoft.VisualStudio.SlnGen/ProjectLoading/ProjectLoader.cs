// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents a class for load projects.
    /// </summary>
    public static class ProjectLoader
    {
        /// <summary>
        /// Gets a shared <see cref="EvaluationContext" /> object to use.
        /// </summary>
        public static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);

        /// <summary>
        /// Loads projects.
        /// </summary>
        /// <param name="msbuildExeFileInfo">The <see cref="FileInfo" /> of MSBuild.exe.</param>
        /// <param name="projectCollection">The <see cref="ProjectCollection" /> to use when loading projects.</param>
        /// <param name="entryProjects">The <see cref="IEnumerable{String}" /> of entry projects.</param>
        /// <param name="globalProperties">The <see cref="IDictionary{String,String}" /> of global properties to use.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use as a logger.</param>
        /// <returns>A <see cref="Tuple{TimeSpan, Int32}" /> with the amount of time it took to load projects and the total number of projects that were loaded.</returns>
        public static (TimeSpan projectEvaluation, int projectCount) LoadProjects(FileInfo msbuildExeFileInfo, ProjectCollection projectCollection, IEnumerable<string> entryProjects, IDictionary<string, string> globalProperties, ISlnGenLogger logger)
        {
            if (logger.HasLoggedErrors)
                return (TimeSpan.Zero, 0);

            logger.LogMessageHigh("Loading project references...");

            Stopwatch sw = Stopwatch.StartNew();

            IProjectLoader projectLoader = Create(msbuildExeFileInfo, logger);

            try
            {
                projectLoader.LoadProjects(entryProjects, projectCollection, globalProperties);
            }
            catch (InvalidProjectFileException)
            {
                return (TimeSpan.Zero, 0);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                return (TimeSpan.Zero, 0);
            }

            sw.Stop();

            logger.LogMessageNormal($"Loaded {projectCollection.LoadedProjects.Count:N0} project(s) in {sw.ElapsedMilliseconds:N0}ms");

            return (sw.Elapsed, projectCollection.LoadedProjects.Count);
        }

        /// <summary>
        /// Logs a <see cref="ProjectStartedEventArgs" /> object for the specified project.
        /// </summary>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> to use.</param>
        /// <param name="projectInstance">The <see cref="ProjectInstance" /> of the project.</param>
        internal static void LogProjectStartedEvent(ISlnGenLogger logger, ProjectInstance projectInstance)
        {
            if (!logger.IsDiagnostic)
                return;

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

        /// <summary>
        /// Creates an appropriate instance of a class that implements <see cref="IProjectLoader" />.
        /// </summary>
        /// <param name="msbuildExePath">The full path to MSBuild.exe.</param>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> object to use for logging.</param>
        /// <returns>An <see cref="IProjectLoader" /> object that can be used to load MSBuild projects.</returns>
        private static IProjectLoader Create(FileInfo msbuildExePath, ISlnGenLogger logger)
        {
#if !NETFRAMEWORK
            return new ProjectGraphProjectLoader(logger);
#elif NET461
            return new LegacyProjectLoader(logger);
#else
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(msbuildExePath.FullName);

            // MSBuild 16.4 and above use the Static Graph API
            if (fileVersionInfo.FileMajorPart > 16 || (fileVersionInfo.FileMajorPart == 16 && fileVersionInfo.FileMinorPart >= 4))
            {
                return new ProjectGraphProjectLoader(logger);
            }

            return new LegacyProjectLoader(logger);
#endif
        }
    }
}