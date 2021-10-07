// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represent the current development environment.
    /// </summary>
    public sealed class DevelopmentEnvironment
    {
        private DevelopmentEnvironment(params string[] errors)
            : this()
        {
            Errors = errors.ToList();
        }

        private DevelopmentEnvironment()
        {
            Errors = Array.Empty<string>();
        }

        /// <summary>
        /// Gets the current .NET SDK major version.
        /// </summary>
        public string DotNetSdkMajorVersion { get; private set; }

        /// <summary>
        /// Gets the current .NET SDK version.
        /// </summary>
        public string DotNetSdkVersion { get; private set; }

        /// <summary>
        /// Gets any errors encountered while determining the development environment.
        /// </summary>
        public IReadOnlyCollection<string> Errors { get; }

        /// <summary>
        /// Gets a value indicating whether the current build environment is CoreXT.
        /// </summary>
        public bool IsCorext { get; private set; }

        /// <summary>
        /// Gets a <see cref="FileInfo" /> object for the MSBuild.dll that was found.
        /// </summary>
        public FileInfo MSBuildDll { get; private set; }

        /// <summary>
        /// Gets a <see cref="FileInfo" /> object for the MSBuild.exe that was found.
        /// </summary>
        public FileInfo MSBuildExe { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current development environment was successfully discovered.
        /// </summary>
        public bool Success => Errors?.Count == 0;

        /// <summary>
        /// Gets the <see cref="VisualStudioInstance" /> for the current development environment.
        /// </summary>
        public VisualStudioInstance VisualStudio { get; private set; }

        /// <summary>
        /// Loads the current development environment.
        /// </summary>
        /// <returns>A <see cref="DevelopmentEnvironment" /> object containing information about the current development environment.</returns>
        public static DevelopmentEnvironment LoadCurrentDevelopmentEnvironment()
        {
            string msbuildToolset = Environment.GetEnvironmentVariable("MSBuildToolset")?.Trim();

            if (!msbuildToolset.IsNullOrWhiteSpace())
            {
                string msbuildToolsPath = Environment.GetEnvironmentVariable($"MSBuildToolsPath_{msbuildToolset}")?.Trim();

                if (!msbuildToolsPath.IsNullOrWhiteSpace())
                {
#if NETFRAMEWORK
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
#else
                    return new DevelopmentEnvironment("The .NET Core version of SlnGen is not supported in CoreXT.  You must use the .NET Framework version via the SlnGen.Corext package");
#endif
                }
            }
#if NETFRAMEWORK
            if (Utility.TryFindOnPath("MSBuild.exe", IsMSBuildExeCompatible, out FileInfo msbuildExeFileInfo))
            {
                return new DevelopmentEnvironment
                {
                    MSBuildExe = GetPathToMSBuildExe(msbuildExeFileInfo),
                    VisualStudio = VisualStudioConfiguration.GetInstanceForPath(msbuildExeFileInfo.FullName),
                };
            }

            return new DevelopmentEnvironment("SlnGen must be run from a command-line window where MSBuild.exe is on the PATH.");
#else
            if (!Utility.TryFindOnPath(Utility.RunningOnWindows ? "dotnet.exe" : "dotnet", null, out FileInfo dotnetFileInfo))
            {
                return new DevelopmentEnvironment("SlnGen must be run from a command-line window where dotnet.exe is on the PATH.");
            }

            if (!DotNetCoreSdkResolver.TryResolveDotNetCoreSdk(dotnetFileInfo, out DirectoryInfo dotnetCoreSdkDirectoryInfo))
            {
                return new DevelopmentEnvironment(string.Empty);
            }

            DevelopmentEnvironment developmentEnvironment = new DevelopmentEnvironment
            {
                DotNetSdkVersion = dotnetCoreSdkDirectoryInfo.Name,
                DotNetSdkMajorVersion = dotnetCoreSdkDirectoryInfo.Name.Substring(0, dotnetCoreSdkDirectoryInfo.Name.IndexOf(".", StringComparison.OrdinalIgnoreCase)),
                MSBuildDll = new FileInfo(Path.Combine(dotnetCoreSdkDirectoryInfo.FullName, "MSBuild.dll")),
            };

            if (Utility.RunningOnWindows && Utility.TryFindOnPath("MSBuild.exe", IsMSBuildExeCompatible, out FileInfo msbuildExeFileInfo))
            {
                developmentEnvironment.MSBuildExe = GetPathToMSBuildExe(msbuildExeFileInfo);
                developmentEnvironment.VisualStudio = VisualStudioConfiguration.GetInstanceForPath(msbuildExeFileInfo.FullName);
            }

            return developmentEnvironment;
#endif
        }

        private static bool IsMSBuildExeCompatible(FileInfo fileInfo)
        {
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

                return fileVersionInfo.FileMajorPart >= 15;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the path to MSBuild.exe based on the current processor architecture.
        /// </summary>
        /// <param name="msbuildExeFileInfo">A <see cref="FileInfo" /> containing the path to an existing MSBuild.exe</param>
        /// <returns>The path to an MSBuild.exe for the current architecture.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="msbuildExeFileInfo"/> is <code>null</code>.</exception>
        private static FileInfo GetPathToMSBuildExe(FileInfo msbuildExeFileInfo)
        {
            if (msbuildExeFileInfo == null)
            {
                throw new ArgumentNullException(nameof(msbuildExeFileInfo));
            }

            AssemblyName assemblyName = AssemblyName.GetAssemblyName(msbuildExeFileInfo.FullName);

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && assemblyName.ProcessorArchitecture != ProcessorArchitecture.Amd64)
            {
                return new FileInfo(Path.Combine(msbuildExeFileInfo.DirectoryName!, "amd64", msbuildExeFileInfo.Name));
            }

            return msbuildExeFileInfo;
        }
    }
}