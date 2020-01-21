// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnGen.ConsoleApp
{
    public sealed class ProgramArguments
    {
        [Option("-bl|--binarylogger <parameters>", CommandOptionType.SingleOrNoValue, Description = @"Serializes all build events to a compressed binary file.
By default the file is in the current directory and named ""slngen.binlog"" and contains the source text of project files, including all imported projects and target files encountered during the build. The optional ProjectImports switch controls this behavior:

 ProjectImports=None     - Don't collect the project imports.
 ProjectImports=Embed    - Embed project imports in the log file.
 ProjectImports=ZipFile  - Save project files to output.projectimports.zip where output is the same name as the binary log file name.

NOTE: The binary logger does not collect non-MSBuild source files such as .cs, .cpp etc.

Example: -bl:output.binlog;ProjectImports=ZipFile")]
        public (bool HasValue, string Arguments) BinaryLogger { get; set; }

        [Option("-clp|--consoleloggerparameters <parameters>", CommandOptionType.SingleOrNoValue, Description = @"Parameters to console logger. The available parameters are:
    PerformanceSummary--Show time spent in tasks, targets and projects.
    Summary--Show error and warning summary at the end.
    NoSummary--Don't show error and warning summary at the end.
    ErrorsOnly--Show only errors.
    WarningsOnly--Show only warnings.
    ShowTimestamp--Display the Timestamp as a prefix to any message.
    ShowEventId--Show eventId for started events, finished events, and messages
    ForceNoAlign--Does not align the text to the size of the console buffer
    DisableConsoleColor--Use the default console colors for all logging messages.
    ForceConsoleColor--Use ANSI console colors even if console does not support it
    Verbosity--overrides the -verbosity setting for this logger.
 Example:
    --consoleloggerparameters:PerformanceSummary;NoSummary;Verbosity=Minimal")]
        public (bool HasValue, string Arguments) ConsoleLoggerParameters { get; set; }

        [Option("--debug", CommandOptionType.NoValue, Description = "Debug the application", ShowInHelpText = false)]
        public bool Debug { get; set; }

        [Option(CommandOptionType.SingleValue, Template = "-d|--devenvfullpath", Description = "Specifies a full path to Visual Studio’s devenv.exe to use when opening the solution file. By default, SlnGen will launch the program associated with the .sln file extension.")]
        public string DevEnvFullPath { get; set; }

        [Option("-flp|--fileloggerparameters <parameters>", CommandOptionType.SingleOrNoValue, Description = @"Provides any extra parameters for file loggers. The same parameters listed for the console logger are available.
Some additional available parameters are:
    LogFile--path to the log file into which the build log will be written.
    Append--determines if the build log will be appended to or overwrite the log file.Setting the switch appends the build log to the log file;
        Not setting the switch overwrites the contents of an existing log file. The default is not to append to the log file.
    Encoding--specifies the encoding for the file, for example, UTF-8, Unicode, or ASCII
 Examples:
    -fileLoggerParameters:LogFile=MyLog.log;Append;Verbosity=Diagnostic;Encoding=UTF-8")]
        public (bool HasValue, string Arguments) FileLoggerParameters { get; set; }

        [Option("-f|--folders", CommandOptionType.SingleOrNoValue, Description = "Enables the creation of hierarchical solution folders.  Default: false", ValueName = "true|false")]
        public bool Folders { get; set; }

        [Option(CommandOptionType.SingleOrNoValue, Template = "-l|--launch", Description = "Launch Visual Studio after generating the Solution file.  Default: true", ValueName = "true|false")]
        public bool LaunchVisualStudio { get; set; } = true;

        [Option("--logger", CommandOptionType.MultipleValue, Description = @"Use this logger to log events from SlnGen. To specify multiple loggers, specify each logger separately.
The <logger> syntax is:
  [<class>,]<assembly>[;<parameters>]
The <logger class> syntax is:
  [<partial or full namespace>.]<logger class name>
The <logger assembly> syntax is:
  {<assembly name>[,<strong name>] | <assembly file>}
Logger options specify how SlnGen creates the logger. The <logger parameters> are optional, and are passed to the logger exactly as you typed them.
Examples:
  -logger:XMLLogger,MyLogger,Version=1.0.2,Culture=neutral
  -logger:XMLLogger,C:\Loggers\MyLogger.dll;OutputAsHTML")]
        public string[] Loggers { get; set; }

        /// <summary>
        /// Gets or sets the full path to the project to generate a Visual Studio Solution File for.
        /// </summary>
        [Argument(0, "project path", "An optional path to a project.  If not specified, all projects in the current directory will be used.")]
        public string[] Projects { get; set; }

        [Option("-o|--solutionfile", Description = "An optional path to the solution file to generate.  Defaults to the same directory as the project.")]
        public string SolutionFileFullPath { get; set; }

        [Option(CommandOptionType.SingleOrNoValue, Template = "-u|--useshellexecute", Description = "Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler.  Default: true", ValueName = "true|false")]
        public bool UseShellExecute { get; set; } = true;

        [Option("-v|--verbosity", CommandOptionType.SingleValue, Description = @"Display this amount of information in the event log.
The available verbosity levels are: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].")]
        public string Verbosity { get; set; }

        public IEnumerable<string> GetProjects(ISlnGenLogger logger)
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

            foreach (string projectPath in Projects)
            {
                logger.LogMessageNormal("Generating solution for project \"{0}\"", projectPath);

                yield return projectPath;
            }
        }

        private void OnExecute()
        {
            SolutionFileGenerator.Generate(this);
        }
    }
}