// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class for resolving .NET Core SDKs.
    /// </summary>
    public static class DotNetCoreSdkResolver
    {
        private const string HostFxr = "hostfxr";

        private static readonly Regex DotNetBasePathRegex = new (@"^ Base Path:\s+(?<Path>.*)$");

        /// <summary>
        /// Attempts to locate the .NET Core SDK for the current working directory.
        /// </summary>
        /// <param name="environmentProvider">An <see cref="IEnvironmentProvider" /> to use when accessing the environment.</param>
        /// <param name="dotnetFileInfo">A <see cref="FileInfo" /> representing the path to dotnet.exe.</param>
        /// <param name="developmentEnvironment">Receives a <see cref="DevelopmentEnvironment" /> instance containing details of the .NET Core SDK if one is found.</param>
        /// <returns><code>true</code> if a .NET Core SDK could be located, otherwise <code>false</code>.</returns>
        public static bool TryResolveDotNetCoreSdk(IEnvironmentProvider environmentProvider, FileInfo dotnetFileInfo, out DevelopmentEnvironment developmentEnvironment)
        {
            if (environmentProvider is null)
            {
                throw new ArgumentNullException(nameof(environmentProvider));
            }

            string parsedBasePath = null;

            using ManualResetEvent processExited = new ManualResetEvent(false);
            using Process process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = "--info",
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WorkingDirectory = environmentProvider.CurrentDirectory,
                },
            };

            process.StartInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = bool.TrueString;

            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    Match match = DotNetBasePathRegex.Match(args.Data);

                    if (match.Success && match.Groups["Path"].Success)
                    {
                        parsedBasePath = match.Groups["Path"].Value.Trim();
                    }
                }
            };

            process.Exited += (_, _) =>
            {
                processExited.Set();
            };

            try
            {
                if (!process.Start())
                {
                    developmentEnvironment = new DevelopmentEnvironment("Failed to resolve the .NET SDK.  Verify the 'dotnet' command is available on the PATH.");

                    return false;
                }
            }
            catch (Exception e)
            {
                developmentEnvironment = new DevelopmentEnvironment("Failed to resolve the .NET SDK.  An exception occurred running the 'dotnet --info' command: ", e.ToString());

                return false;
            }

            process.BeginOutputReadLine();

            switch (WaitHandle.WaitAny(new WaitHandle[] { processExited }, TimeSpan.FromSeconds(5)))
            {
                case WaitHandle.WaitTimeout:
                    break;

                case 0:
                    break;
            }

            if (!process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // Ignored
                }
            }

            DirectoryInfo basePath;

            if (!string.IsNullOrWhiteSpace(parsedBasePath))
            {
                basePath = new DirectoryInfo(parsedBasePath);
            }
            else
            {
                (string sdkDirectory, string globalJsonPath, string requestedVersionNumber) = ResolveSdk(environmentProvider, dotnetFileInfo.Directory);

                if (string.IsNullOrWhiteSpace(sdkDirectory))
                {
                    developmentEnvironment = new DevelopmentEnvironment($"Failed to resolve the .NET SDK.  The 'dotnet --info' command returned the path '{parsedBasePath}' and the .NET SDK resolver returned the path '{sdkDirectory}', a global.json path of '{globalJsonPath}', and a requested version of '{requestedVersionNumber}'.");

                    return false;
                }

                basePath = new DirectoryInfo(sdkDirectory);
            }

            developmentEnvironment = new DevelopmentEnvironment
            {
                DotNetSdkVersion = basePath.Name,
                DotNetSdkMajorVersion = basePath.Name.Substring(0, basePath.Name.IndexOf(".", StringComparison.OrdinalIgnoreCase)),
                MSBuildDll = new FileInfo(Path.Combine(basePath.FullName, "MSBuild.dll")),
            };

            return true;
        }

        private static (string sdkDirectory, string globalJsonPath, string requestedVersion) ResolveSdk(IEnvironmentProvider environmentProvider, DirectoryInfo dotnetExeDirectory)
        {
            string sdkDirectory = null;
            string globalJsonPath = null;
            string requestedVersionNumber = null;

            // Set the console color to red in case the .NET SDK resolver logs any errors to the console
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;

            try
            {
                if (Utility.RunningOnWindows)
                {
                    Windows.ResolveSdk(dotnetExeDirectory.FullName, environmentProvider.CurrentDirectory, 0 /* None */, HandleResolveSdkResult);
                }
                else
                {
                    Unix.ResolveSdk(dotnetExeDirectory.FullName, environmentProvider.CurrentDirectory, 0 /* None */, HandleResolveSdkResult);
                }

                return (sdkDirectory, globalJsonPath, requestedVersionNumber);
            }
            finally
            {
                Console.ResetColor();
            }

            void HandleResolveSdkResult(int key, string value)
            {
                switch (key)
                {
                    case 0: // ResolvedSdkDirectory
                        sdkDirectory = value;
                        break;

                    case 1: // GlobalJsonPath
                        globalJsonPath = value;
                        break;

                    case 2: // RequestedVersion
                        requestedVersionNumber = value;
                        break;
                }
            }
        }

        private static class Unix
        {
            private const CharSet UTF8 = CharSet.Ansi;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF8)]
            public delegate void HandleResolveSdkResult(int key, string value);

            //// https://github.com/dotnet/dotnet/blob/83d91d61d4a5f16ceaef2e6f3e5f18970e5d2d27/src/runtime/src/native/corehost/fxr/hostfxr.cpp#L237
            [DllImport(HostFxr, EntryPoint = "hostfxr_resolve_sdk2", CharSet = UTF8, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int ResolveSdk(string dotnetExeDirectory, string workingDirectory, int flags, HandleResolveSdkResult handleSdkResult);
        }

        private static class Windows
        {
            private const CharSet UTF16 = CharSet.Unicode;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = UTF16)]
            public delegate void HandleResolveSdkResult(int key, string value);

            //// https://github.com/dotnet/dotnet/blob/83d91d61d4a5f16ceaef2e6f3e5f18970e5d2d27/src/runtime/src/native/corehost/fxr/hostfxr.cpp#L237
            [DllImport(HostFxr, EntryPoint = "hostfxr_resolve_sdk2", CharSet = UTF16, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int ResolveSdk(string dotnetExeDirectory, string workingDirectory, int flags, HandleResolveSdkResult handleSdkResult);
        }
    }
}