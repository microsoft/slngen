// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;

namespace Microsoft.VisualStudio.SlnGen.Launcher
{
    /// <summary>
    /// Factory to fetch the <see cref="IVisualStudioLauncher"/> instance for the current OS.
    /// </summary>
    internal static class VisualStudioLauncherFactory
    {
        /// <summary>
        /// Fetch the <see cref="IVisualStudioLauncher"/> instance for the current OS.
        /// </summary>
        /// <param name="logger">Current logger instance of the application</param>
        /// <param name="environmentProvider">An <see cref="IEnvironmentProvider" /> instance to use when accessing the environment.</param>
        /// <returns>The Visual Studio launcher for the current executing operating system, null if unsupported</returns>
        internal static IVisualStudioLauncher FetchLauncher(ISlnGenLogger logger, IEnvironmentProvider environmentProvider)
        {
            if (Utility.RunningOnWindows)
            {
                return new VisualStudioLauncherWindows(logger, environmentProvider);
            }

            if (Utility.RunningOnMacOS)
            {
                return new VisualStudioLauncherMac(logger);
            }

            logger.LogWarning("Launching Visual Studio is not currently supported on your operating system.");
            return null;
        }
    }
}