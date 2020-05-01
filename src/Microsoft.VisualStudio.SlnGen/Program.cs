// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the main program for SlnGen.
    /// </summary>
    public sealed class Program
    {
        private static readonly TelemetryClient TelemetryClient;

        private readonly ProgramArguments _arguments;
        private readonly IConsole _console;
        private readonly VisualStudioInstance _instance;
        private readonly FileInfo _msbuildExePath;

        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }

            TelemetryClient = new TelemetryClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Program"/> class.
        /// </summary>
        /// <param name="arguments">The <see cref="ProgramArguments" /> to use.</param>
        /// <param name="console">The <see cref="IConsole" /> to use.</param>
        /// <param name="instance">The <see cref="VisualStudioInstance" /> to use.</param>
        /// <param name="msbuildBinPath">The full path to MSBuild to use.</param>
        public Program(ProgramArguments arguments, IConsole console, VisualStudioInstance instance, string msbuildBinPath)
        {
            _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _instance = instance;
            _msbuildExePath = new FileInfo(Path.Combine(msbuildBinPath, IsNetCore ? "MSBuild.dll" : "MSBuild.exe"));
        }

        /// <summary>
        /// Gets a value indicating whether or not the curent build environment is CoreXT.
        /// </summary>
        public static bool IsCorext { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the current runtime framework is .NET Core.
        /// </summary>
        public static bool IsNetCore { get; } = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");

        /// <summary>
        /// Executes the programs with the specified arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to use when executing.</param>
        /// <param name="console">A <see cref="IConsole" /> object to use for the console.</param>
        /// <returns>zero if the program executed successfully, otherwise non-zero.</returns>
        public static int Execute(string[] args, IConsole console)
        {
            bool noLogo = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("/?"))
                {
                    args[i] = "--help";
                }

                if (args[i].Equals("/nologo", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--nologo", StringComparison.OrdinalIgnoreCase))
                {
                    noLogo = true;
                }

                // Translate / to - or -- for Windows users
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (args[i][0] == '/')
                    {
                        if (args[i].Length == 2 || (i >= 3 && args.Length > i && args[i].Length >= 3 && args[i][2] == ':'))
                        {
                            args[i] = $"-{args[i].Substring(1)}";
                        }
                        else
                        {
                            args[i] = $"--{args[i].Substring(1)}";
                        }
                    }
                }
            }

            if (!noLogo)
            {
                Console.WriteLine(
                    Strings.Message_Logo,
                    ThisAssembly.AssemblyTitle,
                    ThisAssembly.AssemblyInformationalVersion,
                    IsNetCore ? ".NET Core" : ".NET Framework");
            }

            return CommandLineApplication.Execute<ProgramArguments>(console, args);
        }

        /// <summary>
        /// Executes the program.
        /// </summary>
        /// <returns>The exit code of the program.</returns>
        public int Execute()
        {
            LoggerVerbosity verbosity = ForwardingLogger.ParseLoggerVerbosity(_arguments.Verbosity?.LastOrDefault());

            ConsoleForwardingLogger consoleLogger = new ConsoleForwardingLogger(_console)
            {
                NoWarn = _arguments.NoWarn,
                Parameters = _arguments.ConsoleLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "ForceNoAlign=true;Summary" : _arguments.ConsoleLoggerParameters.Arguments,
                Verbosity = verbosity,
            };

            ForwardingLogger forwardingLogger = new ForwardingLogger(GetLoggers(consoleLogger), _arguments.NoWarn)
            {
                Verbosity = verbosity,
            };

            using (ProjectCollection projectCollection = new ProjectCollection(
                globalProperties: null,
                loggers: new ILogger[]
                {
                    forwardingLogger,
                },
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
#if NET46
                onlyLogCriticalEvents: false))
#else
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true))
#endif
            {
                try
                {
                    forwardingLogger.LogMessageLow("Command Line Arguments: {0}", Environment.CommandLine);

                    forwardingLogger.LogMessageLow("Using MSBuild from \"{0}\"", _msbuildExePath);

                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => i.FullName.StartsWith("Microsoft.Build")))
                    {
                        forwardingLogger.LogMessageLow("Loaded assembly: \"{0}\" from \"{1}\"", assembly.FullName, assembly.Location);
                    }

                    (TimeSpan evaluationTime, int evaluationCount) = LoadProjects(projectCollection, forwardingLogger);

                    if (forwardingLogger.HasLoggedErrors)
                    {
                        return 1;
                    }

                    (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount) = GenerateSolutionFile(projectCollection.LoadedProjects.Where(i => !i.GlobalProperties.ContainsKey("TargetFramework")), forwardingLogger);

                    if (_arguments.ShouldLaunchVisualStudio())
                    {
                        bool loadProjectsInVisualStudio = _arguments.ShouldLoadProjectsInVisualStudio();
                        bool enableShellExecute = _arguments.EnableShellExecute();

                        string devEnvFullPath = _arguments.DevEnvFullPath?.LastOrDefault();

                        if (!enableShellExecute || !loadProjectsInVisualStudio || IsCorext)
                        {
                            if (_instance == null)
                            {
                                forwardingLogger.LogError("Cannot launch Visual Studio.");

                                return 1;
                            }

                            if (_instance.IsBuildTools)
                            {
                                forwardingLogger.LogError("Cannot use a BuildTools instance of Visual Studio.");

                                return 1;
                            }

                            devEnvFullPath = Path.Combine(_instance.InstallationPath, "Common7", "IDE", "devenv.exe");
                        }

                        VisualStudioLauncher.Launch(solutionFileFullPath, loadProjectsInVisualStudio, devEnvFullPath, forwardingLogger);
                    }

                    LogTelemetry(evaluationTime, evaluationCount, customProjectTypeGuidCount, solutionItemCount);
                }
                catch (Exception e)
                {
                    forwardingLogger.LogError($"Unhandled exception: {e}");
                    throw;
                }
            }

            return 0;
        }

        private static int Main(string[] args)
        {
            try
            {
                return Execute(args, PhysicalConsole.Singleton);
            }
            catch (Exception e)
            {
                if (!TelemetryClient.PostException(e))
                {
                    throw;
                }

                return 2;
            }
            finally
            {
                TelemetryClient.Dispose();
            }
        }

        private (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount) GenerateSolutionFile(IEnumerable<Project> projects, ISlnGenLogger logger)
        {
            Project project = projects.First();

            IReadOnlyDictionary<string, Guid> customProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(project);

            IReadOnlyCollection<string> solutionItems = SlnProject.GetSolutionItems(project, logger).ToList();

            string solutionFileFullPath = _arguments.SolutionFileFullPath?.LastOrDefault();

            if (solutionFileFullPath.IsNullOrWhiteSpace())
            {
                string solutionDirectoryFullPath = _arguments.SolutionDirectoryFullPath?.LastOrDefault();

                if (solutionDirectoryFullPath.IsNullOrWhiteSpace())
                {
                    solutionDirectoryFullPath = project.DirectoryPath;
                }

                string solutionFileName = Path.ChangeExtension(Path.GetFileName(project.FullPath), "sln");

                solutionFileFullPath = Path.Combine(solutionDirectoryFullPath, solutionFileName);
            }

            logger.LogMessageHigh($"Generating Visual Studio solution \"{solutionFileFullPath}\" ...");

            if (customProjectTypeGuids.Count > 0)
            {
                logger.LogMessageLow("Custom Project Type GUIDs:");
                foreach (KeyValuePair<string, Guid> item in customProjectTypeGuids)
                {
                    logger.LogMessageLow("  {0} = {1}", item.Key, item.Value);
                }
            }

            SlnFile solution = new SlnFile
            {
                Platforms = _arguments.GetPlatforms(),
                Configurations = _arguments.GetConfigurations(),
            };

            if (SlnFile.TryParseExistingSolution(solutionFileFullPath, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath))
            {
                logger.LogMessageNormal("Updating existing solution file and reusing Visual Studio cache");

                solution.SolutionGuid = solutionGuid;
                solution.ExistingProjectGuids = projectGuidsByPath;

                _arguments.LoadProjectsInVisualStudio = new[] { bool.TrueString };
            }

            solution.AddProjects(projects, customProjectTypeGuids, project.FullPath);

            solution.AddSolutionItems(solutionItems);

            solution.Save(solutionFileFullPath, _arguments.EnableFolders());

            return (solutionFileFullPath, customProjectTypeGuids.Count, solutionItems.Count);
        }

        /// <summary>
        /// Gets specified projects or all projects in the current working directory.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{String}" /> containing the full paths to projects to generate a solution for.</returns>
        private IEnumerable<string> GetEntryProjectPaths(ISlnGenLogger logger)
        {
            if (_arguments.Projects == null || !_arguments.Projects.Any())
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

            foreach (string projectPath in _arguments.Projects.Select(Path.GetFullPath))
            {
                if (!File.Exists(projectPath))
                {
                    logger.LogError(string.Format("Project file \"{0}\" does not exist", projectPath));
                    continue;
                }

                logger.LogMessageNormal("Generating solution for project \"{0}\"", projectPath);

                yield return projectPath;
            }
        }

        private IDictionary<string, string> GetGlobalProperties()
        {
            IDictionary<string, string> globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [MSBuildPropertyNames.BuildingProject] = bool.FalseString,
                [MSBuildPropertyNames.DesignTimeBuild] = bool.TrueString,
                [MSBuildPropertyNames.ExcludeRestorePackageImports] = bool.TrueString,
            };

            if (_arguments.Property != null)
            {
                foreach (KeyValuePair<string, string> item in _arguments.Property.SelectMany(i => i.SplitProperties()))
                {
                    globalProperties[item.Key] = item.Value;
                }
            }

            return globalProperties;
        }

        private IEnumerable<ILogger> GetLoggers(ConsoleForwardingLogger consoleLogger)
        {
            if (consoleLogger != null)
            {
                yield return consoleLogger;
            }

            if (_arguments.FileLoggerParameters.HasValue)
            {
                yield return new FileLogger
                {
                    Parameters = _arguments.FileLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "LogFile=slngen.log;Verbosity=Detailed" : $"LogFile=slngen.log;{_arguments.FileLoggerParameters.Arguments}",
                };
            }

            if (_arguments.BinaryLogger.HasValue)
            {
                foreach (ILogger logger in ForwardingLogger.ParseBinaryLoggerParameters(_arguments.BinaryLogger.Arguments.IsNullOrWhiteSpace() ? "slngen.binlog" : _arguments.BinaryLogger.Arguments))
                {
                    yield return logger;
                }
            }

            foreach (ILogger logger in ForwardingLogger.ParseLoggerParameters(_arguments.Loggers))
            {
                yield return logger;
            }
        }

        private (TimeSpan projectEvaluation, int projectCount) LoadProjects(ProjectCollection projectCollection, ISlnGenLogger logger)
        {
            List<string> entryProjects = GetEntryProjectPaths(logger).ToList();

            if (logger.HasLoggedErrors)
            {
                return (TimeSpan.Zero, 0);
            }

            logger.LogMessageHigh("Loading project references...");

            Stopwatch sw = Stopwatch.StartNew();

            IProjectLoader projectLoader = ProjectLoaderFactory.Create(_msbuildExePath, logger);

            IDictionary<string, string> globalProperties = GetGlobalProperties();

            using (new MSBuildFeatureFlags
            {
                CacheFileEnumerations = true,
                LoadAllFilesAsReadOnly = true,
                MSBuildSkipEagerWildCardEvaluationRegexes = true,
                UseSimpleProjectRootElementCacheConcurrency = true,
                MSBuildExePath = _msbuildExePath.FullName,
            })
            {
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
            }

            sw.Stop();

            logger.LogMessageNormal($"Loaded {projectCollection.LoadedProjects.Count:N0} project(s) in {sw.ElapsedMilliseconds:N0}ms");

            return (sw.Elapsed, projectCollection.LoadedProjects.Count);
        }

        private void LogTelemetry(TimeSpan evaluationTime, int evaluationCount, int customProjectTypeGuidCount, int solutionItemCount)
        {
            string hostName = Dns.GetHostEntry(Environment.MachineName).HostName;

            TelemetryClient.PostEvent(
                "slngen/execute",
                new Dictionary<string, object>
                {
                    ["AssemblyInformationalVersion"] = ThisAssembly.AssemblyInformationalVersion,
                    ["DevEnvFullPathSpecified"] = (!_arguments.DevEnvFullPath?.LastOrDefault().IsNullOrWhiteSpace()).ToString(),
                    ["EntryProjectCount"] = _arguments.Projects?.Length.ToString(),
                    ["Folders"] = _arguments.EnableFolders().ToString(),
                    ["IsCoreXT"] = IsCorext.ToString(),
                    ["IsNetCore"] = IsNetCore.ToString(),
                    ["LaunchVisualStudio"] = _arguments.ShouldLaunchVisualStudio().ToString(),
                    ["SolutionFileFullPathSpecified"] = (!_arguments.SolutionFileFullPath?.LastOrDefault().IsNullOrWhiteSpace()).ToString(),
#if NETFRAMEWORK
                    ["Runtime"] = $".NET Framework {Environment.Version}",
#elif NETCOREAPP
                    ["Runtime"] = $".NET Core {Environment.Version}",
#endif
                    ["UseBinaryLogger"] = _arguments.BinaryLogger.HasValue.ToString(),
                    ["UseFileLogger"] = _arguments.FileLoggerParameters.HasValue.ToString(),
                    ["UseShellExecute"] = _arguments.EnableShellExecute().ToString(),
                    ["CustomProjectTypeGuidCount"] = customProjectTypeGuidCount,
                    ["ProjectCount"] = evaluationCount,
                    ["ProjectEvaluationMilliseconds"] = evaluationTime.TotalMilliseconds,
                    ["SolutionItemCount"] = solutionItemCount,
                },
                new Dictionary<string, object>
                {
                    ["Username"] = hostName.EndsWith("corp.microsoft.com", StringComparison.OrdinalIgnoreCase) ? $"{Environment.UserDomainName}\\{Environment.UserName}" : null,
                });
        }
    }
}