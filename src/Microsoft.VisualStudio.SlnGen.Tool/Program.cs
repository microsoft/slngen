// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.VisualStudio.SlnGen.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    public static class Program
    {
        private const string TargetName = "GenerateVisualStudioSolution";

        private static readonly FileInfo ProjectFileInfo = new FileInfo(Path.Combine(Environment.CurrentDirectory, Path.ChangeExtension(Guid.NewGuid().ToString("N"), ".slngen")));

        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }
        }

        public static int Main(string[] args)
        {
            if (!TryLocateVisualStudio(out string vsInstallDir, out string error))
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(error);
                return -1;
            }

            new XElement(
                    "Project",
                    new XElement(
                        "UsingTask",
                        new XAttribute("TaskName", nameof(SlnGenTask)),
                        new XAttribute("AssemblyFile", typeof(SharedProgram).Assembly.Location)),
                    new XElement(
                        "Target",
                        new XAttribute("Name", TargetName),
                        new XElement(
                            nameof(SlnGenTask),
                            new XAttribute(nameof(SlnGenTask.Args), $"{string.Join(Path.PathSeparator, args)};--nologo"),
                            new XAttribute(nameof(SlnGenTask.VisualStudioInstallDirectory), vsInstallDir))))
                .Save(ProjectFileInfo.FullName);

            try
            {
                ProjectFileInfo.Attributes |= FileAttributes.Hidden;

                return SharedProgram.Main(args, Execute);
            }
            finally
            {
                ProjectFileInfo.Delete();
            }
        }

        private static bool TryLocateVisualStudio(out string vsInstallDir, out string error)
        {
            vsInstallDir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
            error = null;

            if (vsInstallDir.IsNullOrWhiteSpace())
            {
                error = "SlnGen must run from a Visual Studio Developer Command prompt and requires the %VSINSTALLDIR% environment variable to be set.";

                return false;
            }

            if (!Directory.Exists(vsInstallDir))
            {
                error = $"The current Visual Studio Developer Command prompt is configured against a Visual Studio directory that does not exist ({vsInstallDir}).";

                return false;
            }

            return true;
        }

        private static int Execute(ProgramArguments arguments, IConsole console)
        {
            List<string> commandLineArguments = new List<string>
            {
                "msbuild",
                $"\"{ProjectFileInfo.FullName}\"",
                "-noLogo",
                "-noAutoResponse",
                "-restore:false",
                $"-target:{TargetName}",
            };

            if (arguments.ConsoleLoggerParameters.HasValue)
            {
                commandLineArguments.Add($"-consoleLoggerParameters:{(arguments.ConsoleLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "Verbosity=Minimal;Summary;ForceNoAlign" : arguments.ConsoleLoggerParameters.Arguments)}");
            }

            if (arguments.FileLoggerParameters.HasValue)
            {
                commandLineArguments.Add($"-fileLoggerParameters:{(arguments.FileLoggerParameters.Arguments.IsNullOrWhiteSpace() ? "LogFile=slngen.log;Verbosity=Detailed" : $"LogFile=slngen.log;{arguments.FileLoggerParameters.Arguments}")}");
            }

            if (arguments.BinaryLogger.HasValue)
            {
                commandLineArguments.Add($"-binaryLogger:{(arguments.BinaryLogger.Arguments.IsNullOrWhiteSpace() ? "LogFile=slngen.binlog" : $"LogFile=slngen.binlog;{arguments.BinaryLogger.Arguments}")}");
            }

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = string.Join(" ", commandLineArguments),
                    FileName = "dotnet",
                    UseShellExecute = false,
                },
            };

            process.StartInfo.EnvironmentVariables["COMPLUS_GCSERVER"] = "1";
            process.StartInfo.EnvironmentVariables["COREHOST_TRACE"] = "0";
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            process.StartInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";

            if (process.Start())
            {
                try
                {
                    process.WaitForExit();

                    return process.ExitCode;
                }
                finally
                {
                    process.Dispose();
                }
            }

            return -1;
        }
    }
}