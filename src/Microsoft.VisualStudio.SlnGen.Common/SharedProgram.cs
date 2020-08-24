// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    internal static class SharedProgram
    {
        private static readonly TelemetryClient TelemetryClient;

        private static Func<ProgramArguments, IConsole, int> _execute;

        static SharedProgram()
        {
            TelemetryClient = new TelemetryClient();
        }

        public static bool IsCorext { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the current runtime framework is .NET Core.
        /// </summary>
        public static bool IsNetCore { get; } = !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);

        public static void LogTelemetry(ProgramArguments arguments, TimeSpan evaluationTime, int evaluationCount, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid)
        {
            try
            {
                TelemetryClient.PostEvent(
                        "execute",
                        new Dictionary<string, object>
                        {
                            ["AssemblyInformationalVersion"] = ThisAssembly.AssemblyInformationalVersion,
                            ["DevEnvFullPathSpecified"] = (!arguments.DevEnvFullPath?.LastOrDefault().IsNullOrWhiteSpace()).ToString(),
                            ["EntryProjectCount"] = arguments.Projects?.Length.ToString(),
                            ["Folders"] = arguments.EnableFolders().ToString(),
                            ["CollapseFolders"] = arguments.EnableCollapseFolders().ToString(),
                            ["IsCoreXT"] = IsCorext.ToString(),
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
                            ["UseShellExecute"] = arguments.EnableShellExecute().ToString(),
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

        public static int Main(string[] args, Func<ProgramArguments, IConsole, int> execute)
        {
            return Main(args, PhysicalConsole.Singleton, execute);
        }

        public static int Main(ProgramArguments args, IConsole console)
        {
            return _execute(args, console);
        }

        internal static int Main(string[] args, IConsole console, Func<ProgramArguments, IConsole, int> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));

            try
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
    }
}