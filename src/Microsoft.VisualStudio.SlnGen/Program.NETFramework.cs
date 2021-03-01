// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the main logic of the application.
    /// </summary>
    public static partial class Program
    {
        /// <summary>
        /// Gets or sets the <see cref="VisualStudioInstance" /> to use.
        /// </summary>
        public static VisualStudioInstance VisualStudio { get; set; }

        private static ProjectCollection GetProjectCollection(params ILogger[] loggers)
        {
            return new ProjectCollection(
                globalProperties: null,
                loggers: loggers,
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
#if NET46
                onlyLogCriticalEvents: false);
#else
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true);
#endif
        }

        private static DevelopmentEnvironment LoadDevelopmentEnvironmentFromCoreXT(string msbuildToolsPath)
        {
            if (!Version.TryParse(Environment.GetEnvironmentVariable("VisualStudioVersion") ?? string.Empty, out Version visualStudioVersion))
            {
                return new DevelopmentEnvironment("The VisualStudioVersion environment variable must be set in CoreXT");
            }

            if (visualStudioVersion.Major <= 14)
            {
                return new DevelopmentEnvironment("MSBuild.Corext version 15.0 or greater is required");
            }

            return new DevelopmentEnvironment
            {
                MSBuildExe = new FileInfo(Path.Combine(msbuildToolsPath!, "MSBuild.exe")),
                IsCorext = true,
                VisualStudio = VisualStudioConfiguration.GetLaunchableInstances()
                    .Where(i => !i.IsBuildTools && i.HasMSBuild && i.InstallationVersion.Major == visualStudioVersion.Major)
                    .OrderByDescending(i => i.InstallationVersion)
                    .FirstOrDefault(),
            };
        }

        private static DevelopmentEnvironment LoadDevelopmentEnvironmentFromCurrentWindow()
        {
            // TODO: Use MSBuild from command-line argument
            // Use MSBuild on the PATH
            if (Program.TryFindMSBuildOnPath(out string msbuildExePath))
            {
                return new DevelopmentEnvironment
                {
                    MSBuildExe = new FileInfo(msbuildExePath),
                    VisualStudio = VisualStudioConfiguration.GetInstanceForPath(msbuildExePath),
                };
            }

            return new DevelopmentEnvironment("SlnGen must be run from a command-line window where MSBuild.exe is on the PATH.");
        }
    }
}