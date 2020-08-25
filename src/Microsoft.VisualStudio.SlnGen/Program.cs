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

namespace Microsoft.VisualStudio.SlnGen
{
    public static class Program
    {
        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }
        }

        public static ProgramArguments Arguments { get; set; }

        public static FileInfo MSBuildExeFileInfo { get; set; }

        public static VisualStudioInstance VisualStudio { get; set; }

        public static int Main(string[] args)
        {
            if (!TryLocateMSBuild(out VisualStudioInstance instance, out FileInfo msBuildExeFileInfo, out string error))
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(error);
                return -1;
            }

            VisualStudio = instance;
            MSBuildExeFileInfo = msBuildExeFileInfo;

            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                // MSBuild.exe.config has binding redirects that change from time to time and its very hard to make sure that NuGet.Build.Tasks.Console.exe.config is correct.
                // It also can be different per instance of Visual Studio so when running unit tests it always needs to match that instance of MSBuild
                // The code below runs this EXE in an AppDomain as if its MSBuild.exe so the assembly search location is next to MSBuild.exe and all binding redirects are used
                // allowing this process to evaluate MSBuild projects as if it is MSBuild.exe
                var thisAssembly = Assembly.GetExecutingAssembly();

                AppDomain appDomain = AppDomain.CreateDomain(
                    thisAssembly.FullName,
                    securityInfo: null,
                    info: new AppDomainSetup
                    {
                        ApplicationBase = MSBuildExeFileInfo.DirectoryName,
                        ConfigurationFile = Path.Combine(MSBuildExeFileInfo.DirectoryName, Path.ChangeExtension(MSBuildExeFileInfo.Name, ".exe.config")),
                    });

                return appDomain
                    .ExecuteAssembly(
                        thisAssembly.Location,
                        args);
            }

            return SharedProgram.Main(args, Execute);
        }

        private static int Execute(ProgramArguments arguments, IConsole console)
        {
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

                    forwardingLogger.LogMessageLow("Using MSBuild from \"{0}\"", MSBuildExeFileInfo);

                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(i => i.FullName.StartsWith("Microsoft.Build")))
                    {
                        forwardingLogger.LogMessageLow("Loaded assembly: \"{0}\" from \"{1}\"", assembly.FullName, assembly.Location);
                    }

                    if (!arguments.TryGetEntryProjectPaths(forwardingLogger, out IReadOnlyList<string> projectEntryPaths))
                    {
                        return 1;
                    }

                    (TimeSpan evaluationTime, int evaluationCount) = ProjectLoader.LoadProjects(MSBuildExeFileInfo, projectCollection, projectEntryPaths, null, forwardingLogger);

                    if (forwardingLogger.HasLoggedErrors)
                    {
                        return 1;
                    }

                    (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid) = SlnFile.GenerateSolutionFile(arguments, projectCollection.LoadedProjects.Where(i => !i.GlobalProperties.ContainsKey("TargetFramework")), forwardingLogger);

                    if (!VisualStudioLauncher.TryLaunch(arguments, VisualStudio, solutionFileFullPath, forwardingLogger))
                    {
                        return 1;
                    }

                    SharedProgram.LogTelemetry(arguments, evaluationTime, evaluationCount, customProjectTypeGuidCount, solutionItemCount, solutionGuid);
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

        /// <summary>
        /// Attempts to locate MSBuild based on the current environment.
        /// </summary>
        /// <param name="instance">Receives a <see cref="VisualStudioInstance" /> if one could be found.</param>
        /// <param name="msbuildExeFileInfo">Receives the path to MSBuild if it could be found.</param>
        /// <param name="error">Receives an error message when the method is unable to find an instance of MSBuild.</param>
        /// <returns><code>true</code> if an instance of MSBuild could be found, otherwise <code>false</code>.</returns>
        private static bool TryLocateMSBuild(out VisualStudioInstance instance, out FileInfo msbuildExeFileInfo, out string error)
        {
            instance = null;
            msbuildExeFileInfo = null;
            error = null;

            string msbuildToolset = Environment.GetEnvironmentVariable("MSBuildToolset")?.Trim();

            if (!msbuildToolset.IsNullOrWhiteSpace())
            {
                string msbuildToolsPath = Environment.GetEnvironmentVariable($"MSBuildToolsPath_{msbuildToolset}")?.Trim();

                if (!msbuildToolsPath.IsNullOrWhiteSpace())
                {
                    if (SharedProgram.IsNetCore)
                    {
                        error = "The .NET Core version of SlnGen is not supported in CoreXT.  You must use the .NET Framework version via the SlnGen.Corext package";

                        return false;
                    }

                    msbuildExeFileInfo = new FileInfo(Path.Combine(msbuildToolsPath, "MSBuild.exe"));

                    if (!Version.TryParse(Environment.GetEnvironmentVariable("VisualStudioVersion") ?? string.Empty, out Version visualStudioVersion))
                    {
                        error = "The VisualStudioVersion environment variable must be set in CoreXT";

                        return false;
                    }

                    if (visualStudioVersion.Major <= 14)
                    {
                        error = "MSBuild.Corext version 15.0 or greater is required";

                        return false;
                    }

                    instance = VisualStudioConfiguration.GetLaunchableInstances()
                        .Where(i => !i.IsBuildTools && i.HasMSBuild && i.InstallationVersion.Major == visualStudioVersion.Major)
                        .OrderByDescending(i => i.InstallationVersion)
                        .FirstOrDefault();

                    SharedProgram.IsCorext = true;

                    return true;
                }
            }

            string vsInstallDirEnvVar = Environment.GetEnvironmentVariable("VSINSTALLDIR");

            if (vsInstallDirEnvVar.IsNullOrWhiteSpace())
            {
                error = "SlnGen must run from a Visual Studio Developer Command prompt and requires the %VSINSTALLDIR% environment variable to be set.";

                return false;
            }

            if (!Directory.Exists(vsInstallDirEnvVar))
            {
                error = $"The current Visual Studio Developer Command prompt is configured against a Visual Studio directory that does not exist ({vsInstallDirEnvVar}).";

                return false;
            }

            instance = VisualStudioConfiguration.GetInstanceForPath(vsInstallDirEnvVar);

            msbuildExeFileInfo = new FileInfo(Path.Combine(
                instance.InstallationPath,
                "MSBuild",
                instance.InstallationVersion.Major >= 16 ? "Current" : "15.0",
                "Bin",
                "MSBuild.exe"));

            return true;
        }
    }
}