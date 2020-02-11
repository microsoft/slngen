// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.SlnGen.ProjectLoading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents the main program for SlnGen.
    /// </summary>
    public sealed partial class Program
    {
        /// <summary>
        /// Executes the programs with the specified arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to use when executing.</param>
        /// <returns>zero if the program executed successfully, otherwise non-zero.</returns>
        public static int Main(string[] args)
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.CopyrightMessage,
                    ThisAssembly.AssemblyInformationalVersion,
#if NETFRAMEWORK
                    ".NET Framework"));
#else
                    ".NET Core"));
#endif
            return CommandLineApplication.Execute<Program>(args);
        }

        public void OnExecute()
        {
            ForwardingLogger logger = new ForwardingLogger(GetLoggers());

            using (ProjectCollection projectCollection = new ProjectCollection(
                globalProperties: null,
                loggers: new ILogger[]
                {
                    logger,
                },
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true))
            using (TelemetryData telemetryData = new TelemetryData(this))
            {
                LoadProjects(projectCollection, telemetryData, logger);

                GenerateSolutionFile(projectCollection.LoadedProjects.Where(i => !i.GlobalProperties.ContainsKey("TargetFramework")), telemetryData, logger);

                if (LaunchVisualStudio)
                {
                    string devEnvFullPath = DevEnvFullPath;

                    if (!UseShellExecute || !ShouldLoadProjectsInVisualStudio)
                    {
                        devEnvFullPath = Path.Combine(Program.VisualStudioInstance.VisualStudioRootPath, "Common7", "IDE", "devenv.exe");
                    }

                    VisualStudioLauncher.Launch(SolutionFileFullPath, UseShellExecute, ShouldLoadProjectsInVisualStudio, devEnvFullPath, logger);
                }
            }
        }

        private void GenerateSolutionFile(IEnumerable<Project> projects, TelemetryData telemetryData, ISlnGenLogger logger)
        {
            Project project = projects.First();

            IReadOnlyDictionary<string, Guid> customProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(project);

            IReadOnlyCollection<string> solutionItems = SlnProject.GetSolutionItems(project, logger).ToList();

            telemetryData.CustomProjectTypeGuidCount = customProjectTypeGuids.Count;

            telemetryData.SolutionItemCount = solutionItems.Count;

            if (SolutionFileFullPath.IsNullOrWhiteSpace())
            {
                SolutionFileFullPath = Path.ChangeExtension(project.FullPath, ".sln");
            }

            logger.LogMessageHigh($"Generating Visual Studio solution \"{SolutionFileFullPath}\" ...");

            if (customProjectTypeGuids.Count > 0)
            {
                logger.LogMessageLow("Custom Project Type GUIDs:");
                foreach (KeyValuePair<string, Guid> item in customProjectTypeGuids)
                {
                    logger.LogMessageLow("  {0} = {1}", item.Key, item.Value);
                }
            }

            SlnFile solution = new SlnFile();

            solution.AddProjects(projects, customProjectTypeGuids, project.FullPath);

            solution.AddSolutionItems(solutionItems);

            solution.Save(SolutionFileFullPath, Folders);
        }

        /// <summary>
        /// Gets specified projects or all projects in the current working directory.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{String}" /> containing the full paths to projects to generate a solution for.</returns>
        private IEnumerable<string> GetEntryProjectPaths(ISlnGenLogger logger)
        {
            if (Projects == null || !Projects.Any())
            {
                logger.LogMessageNormal("Searching \"{0}\" for projects", Environment.CurrentDirectory);
                bool projectFound = false;

                foreach (string projectPath in Directory.EnumerateFiles(Environment.CurrentDirectory, "*.*proj"))
                {
                    projectFound = true;

                    logger.LogMessageNormal("Generating solution for project \"{0}\"", projectPath);

                    yield return projectPath;
                }

                if (!projectFound)
                {
                    logger.LogError("No projects found in the current directory. Please specify the path to the project you want to generate a solution for.");
                }

                yield break;
            }

            foreach (string projectPath in Projects.Select(Path.GetFullPath))
            {
                logger.LogMessageNormal("Generating solution for project \"{0}\"", projectPath);

                yield return projectPath;
            }
        }

        private IEnumerable<ILogger> GetLoggers()
        {
            LoggerVerbosity verbosity = ForwardingLogger.ParseLoggerVerbosity(Verbosity);

            yield return new ConsoleLogger(verbosity)
            {
                Parameters = ConsoleLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "ForceNoAlign=true;Summary" : ConsoleLoggerParameters.Arguments,
            };

            if (FileLoggerParameters.HasValue)
            {
                yield return new FileLogger
                {
                    Parameters = FileLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "LogFile=slngen.log;Verbosity=Detailed" : FileLoggerParameters.Arguments,
                };
            }

            if (BinaryLogger.HasValue)
            {
                foreach (ILogger logger in ForwardingLogger.ParseBinaryLoggerParameters(BinaryLogger.Arguments.IsNullOrWhiteSpace() ? "slngen.binlog" : BinaryLogger.Arguments))
                {
                    yield return logger;
                }
            }

            foreach (ILogger logger in ForwardingLogger.ParseLoggerParameters(Loggers))
            {
                yield return logger;
            }
        }

        private void LoadProjects(ProjectCollection projectCollection, TelemetryData telemetryData, ISlnGenLogger logger)
        {
            List<string> entryProjects = GetEntryProjectPaths(logger).ToList();

            logger.LogMessageHigh("Loading project references...");

            Stopwatch sw = Stopwatch.StartNew();

            IProjectLoader projectLoader = ProjectLoaderFactory.Create(MSBuildExePath, logger);

            IDictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                [MSBuildPropertyNames.BuildingProject] = bool.FalseString,
                [MSBuildPropertyNames.DesignTimeBuild] = bool.TrueString,
                [MSBuildPropertyNames.ExcludeRestorePackageImports] = bool.TrueString,
            };

            using (new MSBuildFeatureFlags
            {
                CacheFileEnumerations = true,
                LoadAllFilesAsReadOnly = true,
                MSBuildSkipEagerWildCardEvaluationRegexes = true,
                UseSimpleProjectRootElementCacheConcurrency = true,
#if NETFRAMEWORK
                MSBuildExePath = MSBuildExePath,
#endif
            })
            {
                projectLoader.LoadProjects(entryProjects, projectCollection, globalProperties);
            }

            sw.Stop();

            logger.LogMessageNormal($"Loaded {projectCollection.LoadedProjects.Count:N0} project(s) in {sw.ElapsedMilliseconds:N0}ms");

            telemetryData.ProjectEvaluationMilliseconds = sw.ElapsedMilliseconds;
            telemetryData.ProjectEvaluationCount = projectCollection.LoadedProjects.Count;
        }

        private void LogStatistics(LegacyProjectLoader projectLoader, ISlnGenLogger logger)
        {
            logger.LogMessageLow("SlnGen Project Evaluation Performance Summary:");

            foreach (KeyValuePair<string, TimeSpan> item in projectLoader.Statistics.ProjectLoadTimes.OrderByDescending(i => i.Value))
            {
                logger.LogMessageLow($"  {Math.Round(item.Value.TotalMilliseconds, 0)} ms  {item.Key}", MessageImportance.Low);
            }
        }
    }
}