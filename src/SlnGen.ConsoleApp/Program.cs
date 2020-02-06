// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;
using SlnGen.Common;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SlnGen.ConsoleApp
{
    /// <summary>
    /// Represents the main program for SlnGen.
    /// </summary>
    public static class Program
    {
        static Program()
        {
            Configure();
        }

        /// <summary>
        /// Gets the full path to devenv.exe if one was found.
        /// </summary>
        public static VisualStudioInstance VisualStudioInstance { get; private set; }

        public static string MSBuildExePath { get; private set; }

        /// <summary>
        /// Executes the programs with the specified arguments.
        /// </summary>
        /// <param name="args">The command-line arguments to use when executing.</param>
        /// <returns>zero if the program executed successfully, otherwise non-zero.</returns>
        public static int Main(string[] args)
        {
            if (args.Any(i => i.StartsWith("-d") || i.StartsWith("--debug")))
            {
                Debugger.Launch();
            }

            Console.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Strings.CopyrightMessage,
                    ThisAssembly.AssemblyInformationalVersion,
#if NETFRAMEWORK
                    ".NET Framework"));
#else
                    ".NET Core"));
#endif
            return CommandLineApplication.Execute<ProgramArguments>(args);
        }

        private static void Configure()
        {
            string msbuildToolset = Environment.GetEnvironmentVariable("MSBuildToolset")?.Trim();

            if (!msbuildToolset.IsNullOrWhitespace())
            {
                string msbuildToolsPath = Environment.GetEnvironmentVariable($"MSBuildToolsPath_{msbuildToolset}");

                if (!msbuildToolsPath.IsNullOrWhitespace())
                {
                    MSBuildLocator.RegisterMSBuildPath(msbuildToolsPath);

#if NETFRAMEWORK
                    MSBuildExePath = Path.Combine(msbuildToolsPath, "MSBuild.exe");
#endif
                    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                    {
                        AssemblyName assemblyName = new AssemblyName(args.Name);

                        string path = Path.Combine(msbuildToolsPath, $"{assemblyName.Name}.dll");

                        if (!File.Exists(path))
                        {
                            path = Path.Combine(msbuildToolsPath, $"{assemblyName.Name}.exe");

                            if (!File.Exists(path))
                            {
                                return null;
                            }
                        }

                        return Assembly.LoadFrom(path);
                    };
                }

                VisualStudioInstance = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions()
                {
                    DiscoveryTypes = DiscoveryType.VisualStudioSetup,
                }).OrderByDescending(i => i.Version).FirstOrDefault();
            }
            else
            {
                VisualStudioInstance = MSBuildLocator.RegisterDefaults();

#if NETFRAMEWORK
                MSBuildExePath = Path.Combine(VisualStudioInstance.MSBuildPath, "MSBuild.exe");
#endif
            }
        }
    }
}