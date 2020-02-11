// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents a class used to launch Visual Studio.
    /// </summary>
    internal static class VisualStudioLauncher
    {
        private const string DoNotLoadProjectsCommandLineArgument = "/DoNotLoadProjects";

        /// <summary>
        /// Launches Visual Studio.
        /// </summary>
        /// <param name="solutionFileFullPath">The full path to the solution file.</param>
        /// <param name="useShellExecute">A value indicating whether to use shell execute.</param>
        /// <param name="loadProjects">A value indicating whether to load projects in Visual Studio.</param>
        /// <param name="devEnvFullPath">An optional full path to devenv.exe.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        public static void Launch(string solutionFileFullPath, bool useShellExecute, bool loadProjects, string devEnvFullPath, ISlnGenLogger logger)
        {
            if (solutionFileFullPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(solutionFileFullPath));
            }

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();

            ProcessStartInfo processStartInfo;

            if (!devEnvFullPath.IsNullOrWhiteSpace())
            {
                if (!File.Exists(devEnvFullPath))
                {
                    logger.LogError($"The specified path to Visual Studio ({devEnvFullPath}) does not exist or is inaccessible.");

                    return;
                }

                processStartInfo = new ProcessStartInfo
                {
                    FileName = devEnvFullPath,
                };

                commandLineBuilder.AppendFileNameIfNotNull(solutionFileFullPath);

                if (!loadProjects)
                {
                    commandLineBuilder.AppendSwitch(DoNotLoadProjectsCommandLineArgument);
                }
            }
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = solutionFileFullPath,
                    UseShellExecute = true,
                };
            }

            try
            {
                processStartInfo.Arguments = commandLineBuilder.ToString();

                Process process = new Process
                {
                    StartInfo = processStartInfo,
                };

                logger.LogMessageHigh("Launching Visual Studio...");
                logger.LogMessageLow("  FileName = {0}", processStartInfo.FileName);
                logger.LogMessageLow("  Arguments = {0}", processStartInfo.Arguments);
                logger.LogMessageLow("  UseShellExecute = {0}", processStartInfo.UseShellExecute);
                logger.LogMessageLow("  WindowStyle = {0}", processStartInfo.WindowStyle);

                if (!process.Start())
                {
                    logger.LogError("Failed to launch Visual Studio.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to launch Visual Studio. {e.Message}");
            }
        }
    }
}