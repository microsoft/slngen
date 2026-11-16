// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen.Launcher
{
    /// <inheritdoc/>
    internal class VisualStudioLauncherWindows : VisualStudioLauncherBase
    {
        /// <summary>
        /// A <see cref="ISlnGenLogger" /> to use for logging.
        /// </summary>
        private readonly ISlnGenLogger logger;

        /// <summary>
        /// An <see cref="IEnvironmentProvider" /> instance to use when accessing the environment.
        /// </summary>
        private readonly IEnvironmentProvider environmentProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualStudioLauncherWindows"/> class.
        /// </summary>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <param name="environmentProvider">An <see cref="IEnvironmentProvider" /> instance to use when accessing the environment.</param>
        internal VisualStudioLauncherWindows(ISlnGenLogger logger, IEnvironmentProvider environmentProvider)
            : base(logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.environmentProvider = environmentProvider ?? throw new ArgumentNullException(nameof(environmentProvider));
        }

        /// <inheritdoc/>
        internal override Process FetchLaunchProcessInfo(string devEnvFullPath, CommandLineBuilder commandLineBuilder)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = devEnvFullPath,
                    Arguments = commandLineBuilder.ToString(),
                    UseShellExecute = true,
                },
            };
        }

        /// <inheritdoc/>
        internal override bool IsVisualStudioInstallValid(string devEnvFullPath)
        {
            VisualStudioInstance visualStudioInstance = null;

            if (!devEnvFullPath.IsNullOrWhiteSpace())
            {
                visualStudioInstance = VisualStudioConfiguration.GetInstanceForPath(devEnvFullPath);
            }

            if (visualStudioInstance == null)
            {
                logger.LogError("Could not find a Visual Studio installation.  Please run from a command window that has MSBuild.exe on the PATH or specify the full path to devenv.exe via the -vs command-line argument");

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

            return true;
        }
    }
}