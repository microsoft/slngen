// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the main program of SlnGen.
    /// </summary>
    public static partial class Program
    {
        private static readonly Regex DotNetBasePathRegex = new Regex(@"^ Base Path:\s+(?<Path>.*)$");

        private static ProjectCollection GetProjectCollection(params ILogger[] loggers)
        {
            ProjectCollection projectCollection = new ProjectCollection(
                globalProperties: null,
                loggers: loggers,
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true);

            if (CurrentDevelopmentEnvironment.MSBuildExe != null && CurrentDevelopmentEnvironment.MSBuildExe.Exists)
            {
                projectCollection.RemoveAllToolsets();

                foreach ((string toolsVersion, IDictionary<string, string> toolsetProperties) in GetToolsets(CurrentDevelopmentEnvironment.MSBuildExe))
                {
                    projectCollection.AddToolset(
                        new Toolset(
                            toolsVersion: toolsVersion,
                            toolsPath: CurrentDevelopmentEnvironment.MSBuildExe.DirectoryName!,
                            projectCollection: projectCollection,
                            msbuildOverrideTasksPath: null,
                            buildProperties: toolsetProperties));
                }
            }

            return projectCollection;
        }

        private static IEnumerable<(string toolsVersion, IDictionary<string, string> toolsetProperties)> GetToolsets(FileInfo msbuildExePath)
        {
            XDocument document = XDocument.Load($"{msbuildExePath.FullName}.config");

            foreach (XElement toolsetElement in document.Element("configuration")?.Element("msbuildToolsets")?.Elements("toolset") ?? Enumerable.Empty<XElement>())
            {
                ProjectRootElement rootElement = ProjectRootElement.Create(NewProjectFileOptions.None);

                foreach (XElement propertyElement in toolsetElement.Elements("property"))
                {
                    string name = propertyElement.Attribute("name")?.Value;

                    if (!name.IsNullOrWhiteSpace() && !string.Equals("MSBuildToolsPath", name))
                    {
                        rootElement.AddProperty(name, propertyElement.Attribute("value")?.Value);
                    }
                }

                Project project = Project.FromProjectRootElement(rootElement, new ProjectOptions());

                yield return (toolsetElement.Attribute("toolsVersion")?.Value, rootElement.Properties.ToDictionary(i => i.Name, i => project.GetPropertyValue(i.Name)));
            }
        }

        private static DevelopmentEnvironment LoadDevelopmentEnvironmentFromCoreXT(string msbuildToolsPath)
        {
            return new ("The .NET Core version of SlnGen is not supported in CoreXT.  You must use the .NET Framework version via the SlnGen.Corext package");
        }

        private static DevelopmentEnvironment LoadDevelopmentEnvironmentFromCurrentWindow()
        {
            string basePath = null;

            using (ManualResetEvent processExited = new ManualResetEvent(false))
            using (Process process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    Arguments = "--info",
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                },
            })
            {
                process.StartInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
                process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = bool.TrueString;

                process.ErrorDataReceived += (sender, args) => { };

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!String.IsNullOrWhiteSpace(args?.Data))
                    {
                        Match match = DotNetBasePathRegex.Match(args.Data);

                        if (match.Success && match.Groups["Path"].Success)
                        {
                            basePath = match.Groups["Path"].Value.Trim();
                        }
                    }
                };

                process.Exited += (sender, args) => { processExited.Set(); };

                try
                {
                    if (!process.Start())
                    {
                        return new DevelopmentEnvironment("Failed to find .NET Core SDK, could not start dotnet");
                    }
                }
                catch (Exception e)
                {
                    return new DevelopmentEnvironment("Failed to find .NET Core SDK, failed launching dotnet", e.ToString());
                }

                process.StandardInput.Close();

                process.BeginErrorReadLine();
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
                    catch
                    {
                    }
                }

                if (!basePath.IsNullOrWhiteSpace())
                {
                    DevelopmentEnvironment developmentEnvironment = new DevelopmentEnvironment
                    {
                        MSBuildDll = new FileInfo(Path.Combine(basePath, "MSBuild.dll")),
                    };

                    RegisterMSBuildAssemblyResolver(developmentEnvironment);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && TryFindMSBuildOnPath(out string msbuildExePath))
                    {
                        developmentEnvironment.MSBuildExe = new FileInfo(msbuildExePath);
                        developmentEnvironment.VisualStudio = VisualStudioConfiguration.GetInstanceForPath(msbuildExePath);
                    }

                    return developmentEnvironment;
                }

                return new DevelopmentEnvironment("Failed to find .NET Core SDK, ensure you have .NET Core SDK installed and if you have a global.json that it specifies a version you have installed.");
            }
        }

        private static void RegisterMSBuildAssemblyResolver(DevelopmentEnvironment developmentEnvironment)
        {
            if (developmentEnvironment.MSBuildDll == null || !developmentEnvironment.MSBuildDll.Exists)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);

                string candidatePath = Path.Combine(developmentEnvironment.MSBuildDll.DirectoryName, $"{assemblyName.Name}.dll");

                if (File.Exists(candidatePath))
                {
                    return Assembly.LoadFrom(candidatePath);
                }

                return null;
            };
        }
    }
}