// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.SlnGen
{
    internal static class MSBuildLocator
    {
        private static readonly Regex NetCoreBasePathRegex = new Regex(@"^\s+Base Path:\s+(?<Path>.*)$");

        /// <summary>
        /// Attempts to locate MSBuild based on the current environment.
        /// </summary>
        /// <param name="logError">An <see cref="Action{String}" /> which can be used to log an error.</param>
        /// <param name="instance">Receives a <see cref="VisualStudioInstance" /> if one could be found.</param>
        /// <param name="msbuildBinPath">Receives the path to MSBuild if it could be found.</param>
        /// <returns><code>true</code> if an instance of MSBuild could be found, otherwise <code>false</code>.</returns>
        public static bool TryLocate(Action<string> logError, out VisualStudioInstance instance, out string msbuildBinPath)
        {
            instance = null;
            msbuildBinPath = null;

            string msbuildToolset = Environment.GetEnvironmentVariable("MSBuildToolset")?.Trim();

            if (!msbuildToolset.IsNullOrWhiteSpace())
            {
                string msbuildToolsPath = Environment.GetEnvironmentVariable($"MSBuildToolsPath_{msbuildToolset}")?.Trim();

                if (!msbuildToolsPath.IsNullOrWhiteSpace())
                {
                    if (Program.IsNetCore)
                    {
                        logError("The .NET Core version of SlnGen is not supported in CoreXT.  You must use the .NET Framework version via the SlnGen.Corext package");

                        return false;
                    }

                    msbuildBinPath = msbuildToolsPath;

                    if (!Version.TryParse(Environment.GetEnvironmentVariable("VisualStudioVersion") ?? string.Empty, out Version visualStudioVersion))
                    {
                        logError("The VisualStudioVersion environment variable must be set in CoreXT");

                        return false;
                    }

                    if (visualStudioVersion.Major <= 14)
                    {
                        logError("MSBuild.Corext version 15.0 or greater is required");

                        return false;
                    }

                    VisualStudioConfiguration configuration = new VisualStudioConfiguration();

                    instance = configuration.GetLaunchableInstances()
                        .Where(i => !i.IsBuildTools && i.HasMSBuild && i.InstallationVersion.Major == visualStudioVersion.Major)
                        .OrderByDescending(i => i.InstallationVersion)
                        .FirstOrDefault();

                    Program.IsCorext = true;

                    return true;
                }
            }

            if (Program.IsNetCore)
            {
                if (!TryGetMSBuildInNetCore(out msbuildBinPath, out string errorMessage))
                {
                    logError(!string.IsNullOrWhiteSpace(errorMessage) ? $"Failed to find .NET Core: {errorMessage}" : "Failed to find .NET Core.  Run dotnet --info for more information.");

                    return false;
                }

                return true;
            }

            if (!TryGetVisualStudioFromDeveloperConsole(out instance) || instance == null)
            {
                logError("You must run SlnGen in a Visual Studio Developer Command Prompt");

                return false;
            }

            msbuildBinPath = Path.Combine(
                instance.InstallationPath,
                "MSBuild",
                instance.InstallationVersion.Major >= 16 ? "Current" : "15.0",
                "Bin");

            return true;
        }

        /// <summary>
        /// Attempts to determine the path to the .NET Core SDK from the output of dotnet --info.
        /// </summary>
        /// <param name="reader">A <see cref="StreamReader" /> that contains the output of dotnet --info</param>
        /// <param name="basePath">Receives the path to the .NET Core SDK if one is found.</param>
        /// <returns><code>true</code> if the path to the .NET Core SDK could be found, otherwise <code>false</code>.</returns>
        internal static bool TryGetDotNetCoreBasePath(StreamReader reader, out string basePath)
        {
            basePath = null;

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(" ", StringComparison.Ordinal))
                {
                    continue;
                }

                Match match = NetCoreBasePathRegex.Match(line);

                if (!match.Success || !match.Groups["Path"].Success)
                {
                    continue;
                }

                basePath = match.Groups["Path"].Value.Trim();

                return true;
            }

            return false;
        }

        private static bool TryGetMSBuildInNetCore(out string msbuildPath, out string error)
        {
            msbuildPath = null;

            error = null;

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = "--info",
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
            };

            // https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet#environment-variables
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE "] = "en-US";
            process.StartInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP "] = "0";
            process.StartInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
            process.StartInfo.EnvironmentVariables["COREHOST_TRACE"] = "0";

            try
            {
                if (!process.Start())
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            if (!process.WaitForExit((int)TimeSpan.FromSeconds(2).TotalMilliseconds))
            {
                return false;
            }

            error = process.StandardError.ReadLine();

            if (!string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            if (TryGetDotNetCoreBasePath(process.StandardOutput, out msbuildPath))
            {
                return true;
            }

            return false;
        }

        private static bool TryGetVisualStudioFromDeveloperConsole(out VisualStudioInstance instance)
        {
            instance = null;

            string vsInstallDirEnvVar = Environment.GetEnvironmentVariable("VSINSTALLDIR");

            if (vsInstallDirEnvVar.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (!Directory.Exists(vsInstallDirEnvVar))
            {
                return false;
            }

            VisualStudioConfiguration configuration = new VisualStudioConfiguration();

            instance = configuration.GetInstanceForPath(vsInstallDirEnvVar);

            return instance != null;
        }
    }
}