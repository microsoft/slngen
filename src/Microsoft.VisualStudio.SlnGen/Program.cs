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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    public static partial class Program
    {
        private static readonly TelemetryClient TelemetryClient;

        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }

            TelemetryClient = new TelemetryClient();
        }

        /// <summary>
        /// Gets or sets the <see cref="ProgramArguments" /> of the current application.
        /// </summary>
        public static ProgramArguments Arguments { get; set; }

        /// <summary>
        /// Gets the current <see cref="DevelopmentEnvironment" />.
        /// </summary>
        public static DevelopmentEnvironment CurrentDevelopmentEnvironment { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the current runtime framework is .NET Core.
        /// </summary>
        public static bool IsNetCore { get; } = !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);

        public static bool NoLogo { get; private set; }

        /// <summary>
        /// Executes the program with the specified command-line arguments.
        /// </summary>
        /// <param name="args">An array of <see cref="string" /> containing the command-line arguments.</param>
        /// <returns>Zero if the program executed successfully, otherwise a non-zero value.</returns>
        public static int Main(string[] args)
        {
            CurrentDevelopmentEnvironment = DevelopmentEnvironment.LoadCurrentDevelopmentEnvironment();

            if (!CurrentDevelopmentEnvironment.Success || CurrentDevelopmentEnvironment.Errors.Count > 0)
            {
                foreach (string error in CurrentDevelopmentEnvironment.Errors)
                {
                    Utility.WriteError(error);
                }

                return -1;
            }

            Assembly thisAssembly = Assembly.GetExecutingAssembly();

#if NETFRAMEWORK
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                AppDomain appDomain = AppDomain.CreateDomain(
                    thisAssembly.FullName,
                    securityInfo: null,
                    info: new AppDomainSetup
                    {
                        ApplicationBase = CurrentDevelopmentEnvironment.MSBuildExe.DirectoryName,
                        ConfigurationFile = Path.Combine(CurrentDevelopmentEnvironment.MSBuildExe.DirectoryName!, Path.ChangeExtension(CurrentDevelopmentEnvironment.MSBuildExe.Name, ".exe.config")),
                    });

                return appDomain
                    .ExecuteAssembly(
                        thisAssembly.Location,
                        args);
            }
#endif
            FileInfo thisAssemblyFileInfo = new FileInfo(thisAssembly.Location);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                AssemblyName requestedAssemblyName = new AssemblyName(eventArgs.Name);

#if NETFRAMEWORK
                FileInfo candidateAssemblyFileInfo = new FileInfo(Path.Combine(thisAssemblyFileInfo.DirectoryName, $"{requestedAssemblyName.Name}.dll"));
#else
                FileInfo candidateAssemblyFileInfo = new FileInfo(Path.Combine(CurrentDevelopmentEnvironment.MSBuildDll.DirectoryName!, $"{requestedAssemblyName.Name}.dll"));
#endif
                if (!candidateAssemblyFileInfo.Exists)
                {
                    return null;
                }

                AssemblyName candidateAssemblyName = AssemblyName.GetAssemblyName(candidateAssemblyFileInfo.FullName);

                if ((requestedAssemblyName.ProcessorArchitecture != ProcessorArchitecture.None && requestedAssemblyName.ProcessorArchitecture != candidateAssemblyName.ProcessorArchitecture)
                    || (requestedAssemblyName.Flags.HasFlag(AssemblyNameFlags.PublicKey) && !requestedAssemblyName.GetPublicKeyToken() !.SequenceEqual(candidateAssemblyName.GetPublicKeyToken() !)))
                {
                    return null;
                }

                return Assembly.LoadFrom(candidateAssemblyFileInfo.FullName);
            };

            return Execute(args, PhysicalConsole.Singleton);
        }

        /// <summary>
        /// Executes the current application with the specified arguments and console.
        /// </summary>
        /// <param name="args">An array of <see cref="string" /> containing the command-line arguments.</param>
        /// <param name="console">A <see cref="IConsole" /> to use.</param>
        /// <param name="execute">A <see cref="Func{ProgramArguments, IConsole, Int32}" /> representing a function to run.</param>
        /// <returns>Zero if the program successfully executed, otherwise non-zero.</returns>
        internal static int Execute(string[] args, IConsole console, Func<ProgramArguments, IConsole, int> execute)
        {
            ProgramArguments.Execute = execute;

            return Execute(args, console);
        }

        /// <summary>
        /// Executes the current application with the specified arguments and console.
        /// </summary>
        /// <param name="arguments">The <see cref="ProgramArguments" /> to use.</param>
        /// <param name="console">A <see cref="IConsole" /> to use.</param>
        /// <returns>Zero if the program successfully executed, otherwise non-zero.</returns>
        internal static int Execute(ProgramArguments arguments, IConsole console)
        {
            if (arguments.Version)
            {
                console.WriteLine(ThisAssembly.AssemblyInformationalVersion);

                return 0;
            }

            MSBuildFeatureFlags featureFlags = new MSBuildFeatureFlags
            {
                CacheFileEnumerations = true,
                LoadAllFilesAsReadOnly = true,
                MSBuildSkipEagerWildCardEvaluationRegexes = true,
                UseSimpleProjectRootElementCacheConcurrency = true,
#if NETFRAMEWORK
                MSBuildExePath = CurrentDevelopmentEnvironment.MSBuildExe.FullName,
#else
                MSBuildExePath = CurrentDevelopmentEnvironment.MSBuildDll.FullName,
#endif
            };

            LoggerVerbosity verbosity = ForwardingLogger.ParseLoggerVerbosity(arguments.Verbosity?.LastOrDefault());

            ConsoleForwardingLogger consoleLogger = new ConsoleForwardingLogger(console)
            {
                NoWarn = arguments.NoWarn,
                Parameters = arguments.ConsoleLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "ForceNoAlign=true;Summary" : arguments.ConsoleLoggerParameters.Arguments,
                Verbosity = verbosity,
            };

            ForwardingLogger forwardingLogger = new ForwardingLogger(GetLoggers(consoleLogger, arguments), arguments.NoWarn)
            {
                Verbosity = verbosity,
            };

            using (ProjectCollection projectCollection = GetProjectCollection(forwardingLogger))
            {
                try
                {
                    forwardingLogger.LogMessageLow("Command Line Arguments: {0}", Environment.CommandLine);

#if !NETFRAMEWORK
                    forwardingLogger.LogMessageLow("Using .NET Core MSBuild from \"{0}\"", CurrentDevelopmentEnvironment.MSBuildDll);
#endif

                    if (CurrentDevelopmentEnvironment.MSBuildExe != null)
                    {
                        forwardingLogger.LogMessageLow("Using .NET Framework MSBuild from \"{0}\"", CurrentDevelopmentEnvironment.MSBuildExe);
                    }

                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => i.FullName.StartsWith("Microsoft.Build")))
                    {
                        forwardingLogger.LogMessageLow("Loaded assembly: \"{0}\" from \"{1}\"", assembly.FullName, assembly.Location);
                    }

                    forwardingLogger.LogMessageLow("Current development environment:");
                    forwardingLogger.LogMessageLow("  FrameworkDescription: \"{0}\"", RuntimeInformation.FrameworkDescription);
                    forwardingLogger.LogMessageLow("  DotNetCoreVersion: \"{0}\"", CurrentDevelopmentEnvironment.DotNetSdkVersion);
                    forwardingLogger.LogMessageLow("  DotNetSdkMajorVersion: \"{0}\"", CurrentDevelopmentEnvironment.DotNetSdkMajorVersion);
                    forwardingLogger.LogMessageLow("  IsCorext: \"{0}\"", CurrentDevelopmentEnvironment.IsCorext);
                    forwardingLogger.LogMessageLow("  MSBuildDll: \"{0}\"", CurrentDevelopmentEnvironment.MSBuildDll);
                    forwardingLogger.LogMessageLow("  MSBuildExe: \"{0}\"", CurrentDevelopmentEnvironment.MSBuildExe);
                    forwardingLogger.LogMessageLow("  VisualStudio: \"{0}\"", CurrentDevelopmentEnvironment.VisualStudio?.InstallationPath);

                    if (!arguments.TryGetEntryProjectPaths(forwardingLogger, out IReadOnlyList<string> projectEntryPaths))
                    {
                        return 1;
                    }

                    (TimeSpan evaluationTime, int evaluationCount) = ProjectLoader.LoadProjects(CurrentDevelopmentEnvironment.MSBuildExe, projectCollection, projectEntryPaths, arguments.GetGlobalProperties(), forwardingLogger);

                    if (forwardingLogger.HasLoggedErrors)
                    {
                        return 1;
                    }

                    (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid) = SlnFile.GenerateSolutionFile(arguments, projectCollection.LoadedProjects.Where(i => !i.GlobalProperties.ContainsKey("TargetFramework")), forwardingLogger);

                    featureFlags.Dispose();

                    if (!VisualStudioLauncher.TryLaunch(arguments, CurrentDevelopmentEnvironment.VisualStudio, solutionFileFullPath, forwardingLogger))
                    {
                        return 1;
                    }

                    Program.LogTelemetry(arguments, evaluationTime, evaluationCount, customProjectTypeGuidCount, solutionItemCount, solutionGuid);
                }
                catch (InvalidProjectFileException e)
                {
                    forwardingLogger.LogError(e.Message, e.ErrorCode, e.ProjectFile, e.LineNumber, e.ColumnNumber);

                    return 1;
                }
                catch (Exception e)
                {
                    forwardingLogger.LogError($"Unhandled exception: {e}");
                    throw;
                }
            }

            return 0;
        }

        private static int Execute(string[] args, IConsole console)
        {
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Equals("/?"))
                    {
                        args[i] = "--help";
                    }

                    if (args[i].Equals("/nologo", StringComparison.OrdinalIgnoreCase) || args[i].Equals("--nologo", StringComparison.OrdinalIgnoreCase))
                    {
                        NoLogo = true;
                    }

                    // Translate / to - or -- for Windows users
                    if (Utility.RunningOnWindows)
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

                if (!NoLogo)
                {
                    console.WriteLine(
                        Strings.Message_Logo,
                        ThisAssembly.AssemblyTitle,
                        ThisAssembly.AssemblyInformationalVersion,
                        IsNetCore ? ".NET Core" : ".NET Framework");
                }

                return CommandLineApplication.Execute<ProgramArguments>(console, args);
            }
            catch (Exception e)
            {
                TelemetryClient.PostException(e);

                Utility.WriteError(e.ToString());

                return 2;
            }
            finally
            {
                TelemetryClient.Dispose();
            }
        }

        private static IEnumerable<ILogger> GetLoggers(ConsoleForwardingLogger consoleLogger, ProgramArguments arguments)
        {
            if (consoleLogger != null)
            {
                yield return consoleLogger;
            }

            if (arguments.FileLoggerParameters.HasValue)
            {
                yield return new FileLogger
                {
                    Parameters = arguments.FileLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "LogFile=slngen.log;Verbosity=Detailed" : $"LogFile=slngen.log;{arguments.FileLoggerParameters.Arguments}",
                };
            }

            if (arguments.BinaryLogger.HasValue)
            {
                foreach (ILogger logger in ForwardingLogger.ParseBinaryLoggerParameters(arguments.BinaryLogger.Arguments.IsNullOrWhiteSpace() ? "slngen.binlog" : arguments.BinaryLogger.Arguments))
                {
                    yield return logger;
                }
            }

            foreach (ILogger logger in ForwardingLogger.ParseLoggerParameters(arguments.Loggers))
            {
                yield return logger;
            }
        }

        private static ProjectCollection GetProjectCollection(params ILogger[] loggers)
        {
            return new ProjectCollection(
                globalProperties: null,
                loggers: loggers,
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
#if NET461
                onlyLogCriticalEvents: false);
#else
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true);
#endif
        }

        private static void LogTelemetry(ProgramArguments arguments, TimeSpan evaluationTime, int evaluationCount, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid)
        {
            try
            {
                TelemetryClient.PostEvent(
                        "execute",
                        new Dictionary<string, object>
                        {
                            ["AlwaysBuild"] = arguments.EnableAlwaysBuild().ToString(),
                            ["AssemblyInformationalVersion"] = ThisAssembly.AssemblyInformationalVersion,
                            ["DevEnvFullPathSpecified"] = (!arguments.DevEnvFullPath?.LastOrDefault().IsNullOrWhiteSpace()).ToString(),
                            ["EntryProjectCount"] = arguments.Projects?.Length.ToString(),
                            ["Folders"] = arguments.EnableFolders().ToString(),
                            ["CollapseFolders"] = arguments.EnableCollapseFolders().ToString(),
                            ["IsCoreXT"] = CurrentDevelopmentEnvironment.IsCorext.ToString(),
                            ["IsNetCore"] = IsNetCore.ToString(),
                            ["LaunchVisualStudio"] = arguments.ShouldLaunchVisualStudio().ToString(),
                            ["SolutionFileFullPathSpecified"] = (!arguments.SolutionFileFullPath?.LastOrDefault().IsNullOrWhiteSpace()).ToString(),
#if NETFRAMEWORK
                            ["Runtime"] = $".NET Framework {Environment.Version}",
#elif NETCOREAPP
                            ["Runtime"] = $".NET Core {Environment.Version}",
#endif
                            ["UseBinaryLogger"] = arguments.BinaryLogger.HasValue.ToString(),
                            ["UseFileLogger"] = arguments.FileLoggerParameters.HasValue.ToString(),
                            ["CustomProjectTypeGuidCount"] = customProjectTypeGuidCount,
                            ["ProjectCount"] = evaluationCount,
                            ["ProjectEvaluationMilliseconds"] = evaluationTime.TotalMilliseconds,
                            ["SolutionItemCount"] = solutionItemCount,
                            ["VS.Platform.Solution.Project.SccProvider.SolutionId"] = solutionGuid == Guid.Empty ? string.Empty : solutionGuid.ToString("B"),
                        });
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}