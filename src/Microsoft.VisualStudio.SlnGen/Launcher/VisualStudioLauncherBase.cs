// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.SlnGen.Launcher
{
    /// <inheritdoc/>
    internal abstract class VisualStudioLauncherBase : IVisualStudioLauncher
    {
        private const string DoNotLoadProjectsCommandLineArgument = "/DoNotLoadProjects";

        /// <summary>
        /// A <see cref="ISlnGenLogger" /> to use for logging.
        /// </summary>
        private readonly ISlnGenLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualStudioLauncherBase"/> class.
        /// </summary>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        internal VisualStudioLauncherBase(ISlnGenLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool TryLaunch(ProgramArguments arguments, VisualStudioInstance visualStudioInstance, string solutionFileFullPath)
        {
            if (!arguments.ShouldLaunchVisualStudio())
            {
                return true;
            }

            string devEnvFullPath = arguments.GetDevEnvFullPath(visualStudioInstance);

            if (!IsVisualStudioInstallValid(devEnvFullPath))
            {
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
                Process process = FetchLaunchProcessInfo(devEnvFullPath, commandLineBuilder);

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

        /// <summary>
        /// Determine if the Visual Studio installation path is valid.
        /// </summary>
        /// <param name="devEnvFullPath">The full path to devenv.exe if one is available, otherwise <c>null</c>.</param>
        /// <returns>A boolean that is true if the Visual Studio installation is valid</returns>
        internal abstract bool IsVisualStudioInstallValid(string devEnvFullPath);

        /// <summary>
        /// Fetch a populated <see cref="Processs"/> object to launch Visual Studio.
        /// </summary>
        /// <param name="devEnvFullPath">The full path to devenv.exe if one is available, otherwise <c>null</c>.</param>
        /// <param name="commandLineBuilder">The command line arguments being passed to Visual Studio's launcher</param>
        /// <returns>A populated <see cref="Process"/> instance to launch Visual Studio with the provided solution</returns>
        internal abstract Process FetchLaunchProcessInfo(string devEnvFullPath, CommandLineBuilder commandLineBuilder);
    }
}