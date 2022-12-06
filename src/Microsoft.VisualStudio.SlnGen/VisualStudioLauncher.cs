// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
        /// <param name="environmentProvider">An <see cref="IEnvironmentProvider" /> instance to use when accessing the environment.</param>
        /// <returns>true if Visual Studio was launched, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="environmentProvider" /> is <c>null</c>.</exception>
        public static bool TryLaunch(ProgramArguments arguments, VisualStudioInstance visualStudioInstance, string solutionFileFullPath, ISlnGenLogger logger, IEnvironmentProvider environmentProvider)
        {
            if (environmentProvider is null)
            {
                throw new ArgumentNullException(nameof(environmentProvider));
            }

            if (!arguments.ShouldLaunchVisualStudio())
            {
                return true;
            }

            if (!Utility.RunningOnWindows)
            {
                logger.LogWarning("Launching Visual Studio is not currently supported on your operating system.");

                return true;
            }

            string devEnvFullPath = arguments.GetDevEnvFullPath(visualStudioInstance);

            if (!devEnvFullPath.IsNullOrWhiteSpace())
            {
                visualStudioInstance = VisualStudioConfiguration.GetInstanceForPath(devEnvFullPath);
            }

            if (visualStudioInstance == null)
            {
                logger.LogError(
                    Program.CurrentDevelopmentEnvironment.IsCorext
                        ? $"Could not find a Visual Studio {environmentProvider.GetEnvironmentVariable("VisualStudioVersion")} installation.  Please do one of the following:\n a) Specify a full path to devenv.exe via the -vs command-line argument\n b) Update your corext.config to specify a version of MSBuild.Corext that matches a Visual Studio version you have installed\n c) Install a version of Visual Studio that matches the version of MSBuild.Corext in your corext.config"
                        : "Could not find a Visual Studio installation.  Please run from a command window that has MSBuild.exe on the PATH or specify the full path to devenv.exe via the -vs command-line argument");

                return false;
            }

            if (visualStudioInstance.IsBuildTools)
            {
                logger.LogError("Cannot use a BuildTools instance of Visual Studio.");

                return false;
            }

            if (!File.Exists(devEnvFullPath))
            {
                logger.LogError($"The specified path to Visual Studio ({devEnvFullPath}) does not exist or is inaccessible.");

                return false;
            }

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();

            commandLineBuilder.AppendFileNameIfNotNull(solutionFileFullPath);

            if (!arguments.ShouldLoadProjectsInVisualStudio())
            {
                commandLineBuilder.AppendSwitch(DoNotLoadProjectsCommandLineArgument);
            }

            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = devEnvFullPath,
                        Arguments = commandLineBuilder.ToString(),
                        UseShellExecute = true,
                    },
                };

                logger.LogMessageHigh("Launching Visual Studio...");
                logger.LogMessageLow("  FileName = {0}", process.StartInfo.FileName);
                logger.LogMessageLow("  Arguments = {0}", process.StartInfo.Arguments);

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