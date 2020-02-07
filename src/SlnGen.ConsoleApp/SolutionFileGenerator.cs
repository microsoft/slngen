// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SlnGen.ConsoleApp
{
    /// <summary>
    /// Generates a Visual Studio (c) Solution file.
    /// </summary>
    public sealed class SolutionFileGenerator : IDisposable
    {
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

        private int Generate()
        {
            using (SlnGenTelemetryData telemetryData = new SlnGenTelemetryData
            {
                DevEnvFullPathSpecified = !_programArguments.DevEnvFullPath.IsNullOrWhitespace(),
                EntryProjectCount = _programArguments.Projects?.Length ?? 0,
                Folders = _programArguments.Folders,
                LaunchVisualStudio = _programArguments.LaunchVisualStudio,
                SolutionFileFullPathSpecified = !_programArguments.SolutionFileFullPath.IsNullOrWhitespace(),
                UseBinaryLogger = _programArguments.BinaryLogger.HasValue,
                UseFileLogger = _programArguments.FileLoggerParameters.HasValue,
                UseShellExecute = _programArguments.UseShellExecute,
            })
            {
                LoadProjects(telemetryData);

                Project project = _projectCollection.LoadedProjects.First();

                Dictionary<string, Guid> customProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(project.GetItems("SlnGenCustomProjectTypeGuid").Select(i => new MSBuildProjectItem(i)));

                IReadOnlyCollection<string> solutionItems = SlnFile.GetSolutionItems(project.GetItems("SlnGenSolutionItem").Select(i => new MSBuildProjectItem(i)), _logger).ToList();

                telemetryData.CustomProjectTypeGuidCount = customProjectTypeGuids.Count;

                telemetryData.SolutionItemCount = solutionItems.Count;

                string solutionFileFullPath = SlnGenUtility.GenerateSolutionFile(
                    _projectCollection,
                    solutionFileFullPath: null,
                    projectFileFullPath: project.FullPath,
                    customProjectTypeGuids: customProjectTypeGuids,
                    folders: _programArguments.Folders,
                    enableConfigurationAndPlatforms: true,
                    solutionItems: solutionItems,
                    _programArguments.GetConfigurations().ToList(),
                    _programArguments.GetPlatforms().ToList(),
                    logger: _logger);

                if (_programArguments.LaunchVisualStudio)
                {
                    string devEnvFullPath = _programArguments.DevEnvFullPath;

                    if (!_programArguments.UseShellExecute || !_programArguments.LoadProjects)
                    {
                        devEnvFullPath = Path.Combine(Program.VisualStudioInstance.VisualStudioRootPath, "Common7", "IDE", "devenv.exe");
                    }

                    SlnGenUtility.LaunchVisualStudio(devEnvFullPath, _programArguments.UseShellExecute, solutionFileFullPath, _programArguments.LoadProjects, _logger);
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

        private void LoadProjects(SlnGenTelemetryData telemetryData)
        {
            _logger.LogMessageHigh("Loading project references...");

            Stopwatch sw = Stopwatch.StartNew();

            var projectLoader = MSBuildProjectLoaderFactory.Create(Program.MSBuildExePath, _logger);

            IDictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["BuildingProject"] = "false",
                ["DesignTimeBuild"] = "true",
                ["ExcludeRestorePackageImports"] = "true",
            };

            projectLoader.LoadProjects(_projectCollection, globalProperties, _programArguments.GetProjects(_logger));

            sw.Stop();

            _logger.LogMessageNormal($"Loaded {_projectCollection.LoadedProjects.Count:N0} project(s) in {sw.ElapsedMilliseconds:N2}ms");

            telemetryData.ProjectEvaluationMilliseconds = sw.ElapsedMilliseconds;
            telemetryData.ProjectEvaluationCount = _projectCollection.LoadedProjects.Count;
        }
    }
}