// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Logging;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SlnGen.ConsoleApp
{
    /// <summary>
    /// Generates a Visual Studio (c) Solution file.
    /// </summary>
    public sealed class SolutionFileGenerator : IDisposable
    {
        private static readonly ProjectLoadSettings DefaultProjectLoadSettings =
            ProjectLoadSettings.IgnoreEmptyImports
            | ProjectLoadSettings.IgnoreInvalidImports
            | ProjectLoadSettings.IgnoreMissingImports
            | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;

        private static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);
        private readonly ISlnGenLogger _logger;
        private readonly ProgramArguments _programArguments;
        private readonly ProjectCollection _projectCollection;

        private SolutionFileGenerator(ProgramArguments programArguments, ISlnGenLogger logger)
        {
            _programArguments = programArguments ?? throw new ArgumentNullException(nameof(programArguments));

            _logger = logger ?? new ForwardingLogger(GetLoggers().ToArray());

            _projectCollection = new ProjectCollection(
                globalProperties: null,
                loggers: new List<ILogger>
                {
                    _logger as ILogger,
                },
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true);
        }

        /// <summary>
        /// Generates a Visual Studio (c) Solution file.
        /// </summary>
        /// <param name="programArguments">The <see cref="ProgramArguments" /> to use when generating the solution.</param>
        /// <param name="logger">An optional <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <returns>zero if it the solution was generated, otherwise non-zero.</returns>
        public static int Generate(ProgramArguments programArguments, ISlnGenLogger logger = null)
        {
            using (SolutionFileGenerator solutionFileGenerator = new SolutionFileGenerator(programArguments, logger))
            {
                return solutionFileGenerator.Generate();
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            (_logger as IDisposable)?.Dispose();

            _projectCollection?.Dispose();
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

        private int Generate()
        {
            using (SlnGenTelemetryData telemetryData = new SlnGenTelemetryData
            {
                DevEnvFullPathSpecified = !_programArguments.DevEnvFullPath.IsNullOrWhitespace(),
                EntryProjectCount = _programArguments.Projects.Length,
                Folders = _programArguments.Folders,
                LaunchVisualStudio = _programArguments.LaunchVisualStudio,
                SolutionFileFullPathSpecified = !_programArguments.SolutionFileFullPath.IsNullOrWhitespace(),
                UseBinaryLogger = _programArguments.BinaryLogger.HasValue,
                UseFileLogger = _programArguments.FileLoggerParameters.HasValue,
                UseShellExecute = _programArguments.UseShellExecute,
            })
            {
                _logger.LogMessageHigh("Loading project references...");

                Stopwatch sw = Stopwatch.StartNew();

                IDictionary<string, string> globalProperties = new Dictionary<string, string>
                {
                    ["BuildingProject"] = "false",
                    ["DesignTimeBuild"] = "true",
                    ["ExcludeRestorePackageImports"] = "true",
                };

                ICollection<ProjectGraphEntryPoint> entryProjects = _programArguments.GetProjects(_logger).Select(i => new ProjectGraphEntryPoint(i, globalProperties)).ToList();

                _ = new ProjectGraph(entryProjects, _projectCollection, CreateProjectInstance);

                sw.Stop();

                _logger.LogMessageNormal($"Loaded {_projectCollection.LoadedProjects.Count} project(s) in {sw.ElapsedMilliseconds:N2}ms");

                telemetryData.ProjectEvaluationMilliseconds = sw.ElapsedMilliseconds;
                telemetryData.ProjectEvaluationCount = _projectCollection.LoadedProjects.Count;

                Project project = _projectCollection.LoadedProjects.First();

                Dictionary<string, Guid> customProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(project.GetItems("SlnGenCustomProjectTypeGuid").Select(i => new MSBuildProjectItem(i)));

                IReadOnlyCollection<string> solutionItems = SlnFile.GetSolutionItems(project.GetItems("SlnGenSolutionItem").Select(i => new MSBuildProjectItem(i)), _logger).ToList();

                telemetryData.CustomProjectTypeGuidCount = customProjectTypeGuids.Count;

                telemetryData.SolutionItemCount = solutionItems.Count;

                string solutionFileFullPath = SlnGenUtility.GenerateSolutionFile(
                    _projectCollection,
                    solutionFileFullPath: null,
                    projectFileFullPath: entryProjects.First().ProjectFile,
                    customProjectTypeGuids: customProjectTypeGuids,
                    folders: _programArguments.Folders,
                    solutionItems: solutionItems,
                    logger: _logger);

                if (_programArguments.LaunchVisualStudio)
                {
                    SlnGenUtility.LaunchVisualStudio(_programArguments.DevEnvFullPath, _programArguments.UseShellExecute, solutionFileFullPath, _logger);
                }

                return _logger.HasLoggedErrors ? 1 : 0;
            }
        }

        private IEnumerable<ILogger> GetLoggers()
        {
            LoggerVerbosity verbosity = ForwardingLogger.ParseLoggerVerbosity(_programArguments.Verbosity);

            yield return new ConsoleLogger(verbosity)
            {
                Parameters = _programArguments.ConsoleLoggerParameters.Arguments.IsNullOrWhitespace() ? "ForceNoAlign=true;Summary" : _programArguments.ConsoleLoggerParameters.Arguments,
            };

            if (_programArguments.FileLoggerParameters.HasValue)
            {
                yield return new FileLogger
                {
                    Parameters = _programArguments.FileLoggerParameters.Arguments.IsNullOrWhitespace() ? "LogFile=slngen.log;Verbosity=Detailed" : _programArguments.FileLoggerParameters.Arguments,
                };
            }

            if (_programArguments.BinaryLogger.HasValue)
            {
                foreach (ILogger logger in ForwardingLogger.ParseBinaryLoggerParameters(_programArguments.BinaryLogger.Arguments.IsNullOrWhitespace() ? "slngen.binlog" : _programArguments.BinaryLogger.Arguments))
                {
                    yield return logger;
                }
            }

            foreach (ILogger logger in ForwardingLogger.ParseLoggerParameters(_programArguments.Loggers))
            {
                yield return logger;
            }
        }
    }
}