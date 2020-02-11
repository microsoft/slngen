// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Locator;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents the main program for SlnGen.
    /// </summary>
    public sealed partial class Program
    {
        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }

            string msbuildToolset = Environment.GetEnvironmentVariable("MSBuildToolset")?.Trim();

            if (!msbuildToolset.IsNullOrWhiteSpace())
            {
                IsCoreXT = true;

                string msbuildToolsPath = Environment.GetEnvironmentVariable($"MSBuildToolsPath_{msbuildToolset}");

                if (!msbuildToolsPath.IsNullOrWhiteSpace())
                {
                    MSBuildLocator.RegisterMSBuildPath(msbuildToolsPath);

                    MSBuildBinPath = msbuildToolsPath;

                    VisualStudioInstance = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions
                    {
                        DiscoveryTypes = DiscoveryType.VisualStudioSetup,
                    }).OrderByDescending(i => i.Version).FirstOrDefault();
                }
            }
            else
            {
                VisualStudioInstance = MSBuildLocator.RegisterDefaults();

                MSBuildBinPath = VisualStudioInstance.MSBuildPath;
            }

            if (!MSBuildBinPath.IsNullOrWhiteSpace())
            {
#if NETFRAMEWORK
                MSBuildExePath = Path.Combine(MSBuildBinPath, "MSBuild.exe");
#else
                MSBuildExePath = Path.Combine(MSBuildBinPath, "MSBuild.dll");
#endif

                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    AssemblyName assemblyName = new AssemblyName(args.Name);

                    string path = Path.Combine(MSBuildBinPath, $"{assemblyName.Name}.dll");

                    if (!File.Exists(path))
                    {
                        path = Path.Combine(MSBuildBinPath, $"{assemblyName.Name}.exe");

                        if (!File.Exists(path))
                        {
                            return null;
                        }
                    }

                    return Assembly.LoadFrom(path);
                };
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the current build environment is CoreXT.
        /// </summary>
        public static bool IsCoreXT { get; }

        /// <summary>
        /// Gets the full path to the MSBuild binary directory.
        /// </summary>
        public static string MSBuildBinPath { get; }

        /// <summary>
        /// Gets the full path to MSBuild.exe.
        /// </summary>
        public static string MSBuildExePath { get; }

        /// <summary>
        /// Gets the full path to devenv.exe if one was found.
        /// </summary>
        public static VisualStudioInstance VisualStudioInstance { get; }
    }
}