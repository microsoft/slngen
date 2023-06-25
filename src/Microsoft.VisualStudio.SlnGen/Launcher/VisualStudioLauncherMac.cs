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
    internal class VisualStudioLauncherMac : VisualStudioLauncherBase
    {
        /// <summary>
        /// A <see cref="ISlnGenLogger" /> to use for logging.
        /// </summary>
        private readonly ISlnGenLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualStudioLauncherMac"/> class.
        /// </summary>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        internal VisualStudioLauncherMac(ISlnGenLogger logger)
            : base(logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        internal override Process FetchLaunchProcessInfo(string devEnvFullPath, CommandLineBuilder commandLineBuilder)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/open",
                    Arguments = string.Format("-a \"{0}\" \"{1}\"", devEnvFullPath, commandLineBuilder.ToString()),
                    UseShellExecute = true,
                },
            };
        }

        /// <inheritdoc/>
        internal override bool IsVisualStudioInstallValid(string devEnvFullPath)
        {
            if (!Directory.Exists(devEnvFullPath))
            {
                logger.LogError($"The specified path to Visual Studio for Mac ({devEnvFullPath}) does not exist or is inaccessible.");

                return false;
            }

            return true;
        }
    }
}