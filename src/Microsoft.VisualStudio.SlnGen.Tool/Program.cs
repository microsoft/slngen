// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the main program of SlnGen.
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
        /// Runs SlnGen with the specified arguments.
        /// </summary>
        /// <param name="args">An array of <see cref="string" /> containing command-line arguments.</param>
        /// <returns>A non-zero number if the program failed, otherwise zero.</returns>
        public static int Main(string[] args)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return WriteError("SlnGen currently only supports Windows.");
                }

                string visualStudioVersionEnvironmentVariable = Environment.GetEnvironmentVariable("VISUALSTUDIOVERSION");

                if (string.IsNullOrWhiteSpace(visualStudioVersionEnvironmentVariable) || !Version.TryParse(visualStudioVersionEnvironmentVariable, out Version visualStudioVersion))
                {
                    visualStudioVersion = new Version(16, 0);
                }

                FileInfo thisAssemblyFileInfo = new FileInfo(typeof(Program).Assembly.Location);

                if (thisAssemblyFileInfo.DirectoryName == null)
                {
                    return WriteError("Unable to find current assembly directory.");
                }

                FileInfo slnGenExe = new FileInfo(Path.Combine(thisAssemblyFileInfo.DirectoryName, "..", "..", visualStudioVersion.Major >= 16 ? "net472" : "net46", "slngen.exe"));

                if (!slnGenExe.Exists)
                {
                    return WriteError("The required component '{0}' was not found. Please check your installation.", slnGenExe.FullName);
                }

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        Arguments = string.Join(" ", args),
                        FileName = slnGenExe.FullName,
                        UseShellExecute = false,
                    },
                };

                if (process.Start())
                {
                    try
                    {
                        process.WaitForExit();

                        return process.ExitCode;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                return WriteError($"Unhandled exception: {e}");
            }

            return -1;
        }

        private static int WriteError(string message, params object[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message, args);
            Console.ResetColor();

            return -1;
        }
    }
}