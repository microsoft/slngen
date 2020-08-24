// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
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
        /// <param name="arguments">The current <see cref="ProgramArguments" />.</param>
        /// <param name="visualStudioInstance">A <see cref="VisualStudioInstance" /> object representing which instance of Visual Studio to launch.</param>
        /// <param name="solutionFileFullPath">The full path to the solution file.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        public static bool TryLaunch(ProgramArguments arguments, VisualStudioInstance visualStudioInstance, string solutionFileFullPath, ISlnGenLogger logger)
        {
            if (!arguments.ShouldLaunchVisualStudio())
            {
                return true;
            }

            bool loadProjectsInVisualStudio = arguments.ShouldLoadProjectsInVisualStudio();
            bool enableShellExecute = arguments.EnableShellExecute();

            string devEnvFullPath = arguments.DevEnvFullPath?.LastOrDefault();

            if (!enableShellExecute || !loadProjectsInVisualStudio || SharedProgram.IsCorext)
            {
                if (devEnvFullPath.IsNullOrWhiteSpace())
                {
                    if (visualStudioInstance == null)
                    {
                        logger.LogError(
                            SharedProgram.IsCorext
                                ? $"Could not find a Visual Studio {Environment.GetEnvironmentVariable("VisualStudioVersion")} installation.  Please do one of the following:\n a) Specify a full path to devenv.exe via the -vs command-line argument\n b) Update your corext.config to specify a version of MSBuild.Corext that matches a Visual Studio version you have installed\n c) Install a version of Visual Studio that matches the version of MSBuild.Corext in your corext.config"
                                : "Could not find a Visual Studio installation.  Please specify the full path to devenv.exe via the -vs command-line argument");

                        return false;
                    }

                    if (visualStudioInstance.IsBuildTools)
                    {
                        logger.LogError("Cannot use a BuildTools instance of Visual Studio.");

                        return false;
                    }

                    devEnvFullPath = Path.Combine(visualStudioInstance.InstallationPath, "Common7", "IDE", "devenv.exe");
                }
            }

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

                    return false;
                }

                processStartInfo = new ProcessStartInfo
                {
                    FileName = devEnvFullPath,
                    UseShellExecute = false,
                };

                commandLineBuilder.AppendFileNameIfNotNull(solutionFileFullPath);

                if (!arguments.ShouldLoadProjectsInVisualStudio())
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

            return true;
        }
    }
}