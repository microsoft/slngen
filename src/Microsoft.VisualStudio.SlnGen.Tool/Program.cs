// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the main logic for the program.
    /// </summary>
    public static class Program
    {
        static Program()
        {
            if (Environment.GetCommandLineArgs().Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }
        }

        /// <summary>
        /// Executes the program.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>0 if the program ran successfully, otherwise a non-zero number.</returns>
        public static int Main(string[] args)
        {
            try
            {
                // Determine the path to the current slngen.exe
                FileInfo thisAssemblyFileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);

                DevelopmentEnvironment developmentEnvironment = DevelopmentEnvironment.LoadCurrentDevelopmentEnvironment();

                if (!developmentEnvironment.Success)
                {
                    foreach (string error in developmentEnvironment.Errors)
                    {
                        Utility.WriteError(error);
                    }

                    // If the development environment couldn't be determined, then we can't proceed since we have no idea what framework to use or MSBuild/dotnet are not available.
                    return 1;
                }

                bool useDotnet = developmentEnvironment.MSBuildExe == null;

                // Default to .NET Framework on Windows if MSBuild.exe is on the PATH
                string framework = Utility.RunningOnWindows && !useDotnet ? "net472" : string.Empty;

                if (useDotnet)
                {
                    switch (developmentEnvironment.DotNetSdkMajorVersion)
                    {
                        case "3":
                            framework = "netcoreapp3.1";
                            break;

                        case "5":
                            framework = "net5.0";
                            break;

                        case "6":
                            framework = "net6.0";
                            break;

                        default:
                            Console.WriteLine($"The .NET SDK {developmentEnvironment.DotNetSdkVersion} is not supported");

                            return -1;
                    }
                }
                else
                {
                    FileVersionInfo msBuildVersionInfo = FileVersionInfo.GetVersionInfo(developmentEnvironment.MSBuildExe.FullName);

                    switch (msBuildVersionInfo.FileMajorPart)
                    {
                        case 15:
                            framework = "net461";
                            break;

                        default:
                            framework = "net472";
                            break;
                    }
                }

                FileInfo slnGenFileInfo = new FileInfo(Path.Combine(thisAssemblyFileInfo.DirectoryName!, "..", thisAssemblyFileInfo.DirectoryName!.EndsWith("any") ? ".." : string.Empty, "slngen", framework, useDotnet ? "slngen.dll" : "slngen.exe"));

                if (!slnGenFileInfo.Exists)
                {
                    Console.WriteLine($"SlnGen not found: {slnGenFileInfo.FullName}");

                    return -1;
                }

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = useDotnet ? "dotnet" : slnGenFileInfo.FullName,
                    },
                };

                if (useDotnet)
                {
                    process.StartInfo.ArgumentList.Add(slnGenFileInfo.FullName);
                }

                foreach (string argument in Environment.GetCommandLineArgs().Skip(1))
                {
                    process.StartInfo.ArgumentList.Add(argument);
                }

                process.Start();

                process.WaitForExit();

                return process.ExitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled exception: {e}");

                return 1;
            }
        }
    }
}