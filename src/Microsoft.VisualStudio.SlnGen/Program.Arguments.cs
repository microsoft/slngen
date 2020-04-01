﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the command-line arguments for this application.
    /// </summary>
    public sealed partial class Program
    {
        /// <summary>
        /// Gets or sets the binary logger arguments.
        /// </summary>
        [Option(
            "-bl|--binarylogger <parameters>",
            CommandOptionType.SingleOrNoValue,
            Description = @"Serializes all build events to a compressed binary file.
By default the file is in the current directory and named ""slngen.binlog"" and contains the source text of project files, including all imported projects and target files encountered during the build. The optional ProjectImports switch controls this behavior:

 ProjectImports=None     - Don't collect the project imports.
 ProjectImports=Embed    - Embed project imports in the log file.
 ProjectImports=ZipFile  - Save project files to output.projectimports.zip where output is the same name as the binary log file name.

NOTE: The binary logger does not collect non-MSBuild source files such as .cs, .cpp etc.

Example: -bl:output.binlog;ProjectImports=ZipFile")]
        public (bool HasValue, string Arguments) BinaryLogger { get; set; }

        /// <summary>
        /// Gets or sets the configurations to use when generating the solution.
        /// </summary>
        [Option(
            "-c|--configuration",
            CommandOptionType.MultipleValue,
            ValueName = "values",
            Description = @"Specifies one or more Configuration values to use when generating the solution.")]
        public string[] Configuration { get; set; }

        /// <summary>
        /// Gets or sets the console logger arguments.
        /// </summary>
        [Option(
            "-cl|--consolelogger <parameters>",
            CommandOptionType.SingleOrNoValue,
            Description = @"Parameters to console logger. The available parameters are:
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

        /// <summary>
        /// Gets or sets a value indicating whether the program should launch the debugger.
        /// </summary>
        [Option(
            "--debug",
            CommandOptionType.NoValue,
            ShowInHelpText = false)]
        public bool Debug { get; set; }

        /// <summary>
        /// Gets or sets the full path to devenv.exe.
        /// </summary>
        [Option(
            "-vs|--devenvfullpath",
            CommandOptionType.MultipleValue,
            Description = "Specifies a full path to Visual Studio’s devenv.exe to use when opening the solution file. By default, SlnGen will launch the program associated with the .sln file extension.")]
        public string[] DevEnvFullPath { get; set; }

        /// <summary>
        /// Gets or sets the file logger parameters.
        /// </summary>
        [Option(
            "-fl|--filelogger <parameters>",
            CommandOptionType.SingleOrNoValue,
            Description = @"Provides any extra parameters for file loggers. The same parameters listed for the console logger are available.
Some additional available parameters are:
    LogFile--path to the log file into which the build log will be written.
    Append--determines if the build log will be appended to or overwrite the log file.Setting the switch appends the build log to the log file;
        Not setting the switch overwrites the contents of an existing log file. The default is not to append to the log file.
    Encoding--specifies the encoding for the file, for example, UTF-8, Unicode, or ASCII
 Examples:
    -fileLoggerParameters:LogFile=MyLog.log;Append;Verbosity=Diagnostic;Encoding=UTF-8")]
        public (bool HasValue, string Arguments) FileLoggerParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether folder hierarchy should be generated in the solution.
        /// </summary>
        [Option(
            "--folders",
            CommandOptionType.MultipleValue,
            ValueName = "true",
            Description = "Enables the creation of hierarchical solution folders.  Default: false")]
        public string[] Folders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Visual Studio should be launched after the solution is generated.
        /// </summary>
        [Option(
            "--launch",
            CommandOptionType.MultipleValue,
            ValueName = "true|false",
            Description = "Launch Visual Studio after generating the Solution file.  Default: true")]
        public string[] LaunchVisualStudio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Visual Studio should load projects.
        /// </summary>
        [Option(
            "--loadprojects",
            CommandOptionType.MultipleValue,
            ValueName = "false",
            Description = @"When launching Visual Studio, opens the specified solution without loading any projects.  Default: true
You must disable shell execute when using this command-line option.
  --useshellexecute:false")]
        public string[] LoadProjectsInVisualStudio { get; set; }

        /// <summary>
        /// Gets or sets the logger parameters.
        /// </summary>
        [Option(
            "--logger",
            CommandOptionType.MultipleValue,
            Description = @"Use this logger to log events from SlnGen. To specify multiple loggers, specify each logger separately.
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
        /// Gets or sets a value indicating whether the logo should be displayed.
        /// </summary>
        [Option(
            "--nologo",
            CommandOptionType.NoValue,
            Description = "Do not display the startup banner and copyright message.")]
        public bool NoLogo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not warning messages should be suppressed..
        /// </summary>
        [Option(
            "--nowarn",
            CommandOptionType.NoValue,
            Description = @"Suppress all warning messages")]
        public bool NoWarn { get; set; }

        /// <summary>
        /// Gets or sets the platforms to use when generating the solution.
        /// </summary>
        [Option(
            "--platform",
            CommandOptionType.MultipleValue,
            ValueName = "values",
            Description = @"Specifies one or more Platform values to use when generating the solution.")]
        public string[] Platform { get; set; }

        /// <summary>
        /// Gets or sets the full path to the project to generate a solution for.
        /// </summary>
        [Argument(
            0,
            Name = "project path",
            Description = "An optional path to a project.  If not specified, all projects in the current directory will be used.")]
        public string[] Projects { get; set; }

        /// <summary>
        /// Gets or sets the platforms to use when generating the solution.
        /// </summary>
        [Option(
            "-p|--property",
            CommandOptionType.MultipleValue,
            ValueName = "name=value[;]",
            Description = @"Set or override these project-level properties. <name> is the property name, and <value> is the property value. Use a semicolon or a comma to separate multiple properties, or specify each property separately.
  Example:
    --property:WarningLevel=2;MyProperty=true")]
        public string[] Property { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the shell should be used when starting the process.
        /// </summary>
        [Option(
            "-u|--useshellexecute",
            CommandOptionType.MultipleValue,
            ValueName = "false",
            Description = "Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler.  Default: true")]
        public string[] ShellExecute { get; set; }

        /// <summary>
        /// Gets or sets the full path to the solution file to generate.
        /// </summary>
        [Option(
            "-o|--solutionfile <path>",
            CommandOptionType.MultipleValue,
            Description = "An optional path to the solution file to generate.  Defaults to the same directory as the project.")]
        public string[] SolutionFileFullPath { get; set; }

        /// <summary>
        /// Gets or sets the full path to the solution file to generate.
        /// </summary>
        [Option(
            "-d|--solutiondir <path>",
            CommandOptionType.MultipleValue,
            Description = "An optional path to the directory in which the solution file will be generated.  Defaults to the same directory as the project. --solutionfile will take precedence over this switch.")]
        public string[] SolutionDirectoryFullPath { get; set; }

        /// <summary>
        /// Gets or sets the verbosity to use.
        /// </summary>
        [Option(
            "-v|--verbosity",
            CommandOptionType.MultipleValue,
            Description = @"Display this amount of information in the event log.  The available verbosity levels are:
  q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].")]
        public string[] Verbosity { get; set; }

        public bool EnableFolders() => GetBoolean(Folders);

        public bool EnableShellExecute() => GetBoolean(ShellExecute, defaultValue: true);

        /// <summary>
        /// Gets the Configuration values based on what was specified as command-line arguments.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyCollection{T}" /> containing the unique values for Configuration.</returns>
        public IReadOnlyCollection<string> GetConfigurations() => Configuration.SplitValues();

        /// <summary>
        /// Gets the Platform values based on what was specified as command-line arguments.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyCollection{T}" /> containing the unique values for Platform.</returns>
        public IReadOnlyCollection<string> GetPlatforms() => Platform.SplitValues();

        public bool ShouldLaunchVisualStudio() => GetBoolean(LaunchVisualStudio, defaultValue: true);

        public bool ShouldLoadProjectsInVisualStudio() => GetBoolean(LoadProjectsInVisualStudio, defaultValue: true);

        private bool GetBoolean(string[] values, bool defaultValue = false)
        {
            if (values == null || values.Length == 0)
            {
                return defaultValue;
            }

            if (bool.TryParse(values.Last(), out bool result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}