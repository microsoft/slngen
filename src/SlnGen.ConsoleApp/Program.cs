// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SlnGen.ConsoleApp
{
    /// <summary>
    /// Represents the main program for SlnGen.
    /// </summary>
    public static class Program
    {
        static Program()
        {
            MSBuildFeatureFlags.EnableCacheFileEnumerations = true;
            MSBuildFeatureFlags.LoadAllFilesAsReadonly = true;
            MSBuildFeatureFlags.SkipEagerWildcardEvaluations = true;
            MSBuildFeatureFlags.EnableSimpleProjectRootElementCache = true;

            VisualStudioInstance instance = MSBuildLocator.RegisterDefaults();

#if NETFRAMEWORK
            MSBuildFeatureFlags.MSBuildExePath = Path.Combine(instance.MSBuildPath, "MSBuild.exe");
#endif
        }

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
    }
}