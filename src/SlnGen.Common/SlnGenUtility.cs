// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SlnGen.Common
{
    public static class SlnGenUtility
    {
        public static readonly char[] EqualsSign = { '=' };

        public static readonly char[] Semicolon = { ';' };

        public static string GenerateSolutionFile(
            ProjectCollection projectCollection,
            string solutionFileFullPath,
            string projectFileFullPath,
            IReadOnlyDictionary<string, Guid> customProjectTypeGuids,
            bool folders,
            bool enableConfigurationAndPlatforms,
            IEnumerable<string> solutionItems,
            IReadOnlyCollection<string> configurations,
            IReadOnlyCollection<string> platforms,
            ISlnGenLogger logger)
        {
            if (string.IsNullOrWhiteSpace(solutionFileFullPath))
            {
                solutionFileFullPath = Path.ChangeExtension(projectFileFullPath, ".sln");
            }

            logger.LogMessageHigh($"Generating Visual Studio solution \"{solutionFileFullPath}\" ...");

            if (customProjectTypeGuids.Count > 0)
            {
                logger.LogMessageLow("Custom Project Type GUIDs:");
                foreach (KeyValuePair<string, Guid> item in customProjectTypeGuids)
                {
                    logger.LogMessageLow("  {0} = {1}", item.Key, item.Value);
                }
            }

            SlnFile solution = new SlnFile
            {
                Configurations = configurations,
                Platforms = platforms,
            };

            solution.AddProjects(projectCollection, customProjectTypeGuids, projectFileFullPath);

            solution.AddSolutionItems(solutionItems);

            solution.Save(solutionFileFullPath, folders, enableConfigurationAndPlatforms);

            return solutionFileFullPath;
        }

        public static void LaunchVisualStudio(string devEnvFullPath, bool useShellExecute, string solutionFileFullPath, bool loadProjects, ISlnGenLogger logger)
        {
            ProcessStartInfo processStartInfo;

            if (!string.IsNullOrWhiteSpace(devEnvFullPath))
            {
                if (!File.Exists(devEnvFullPath))
                {
                    logger.LogError($"The specified path to Visual Studio ({devEnvFullPath}) does not exist or is inaccessible.");

                    return;
                }

                processStartInfo = new ProcessStartInfo
                {
                    FileName = devEnvFullPath,
                    Arguments = $"\"{solutionFileFullPath}\"",
                };

                if (!loadProjects)
                {
                    processStartInfo.Arguments += " /DoNotLoadProjects";
                }
            }
            else if (!useShellExecute || !loadProjects)
            {
                processStartInfo = new ProcessStartInfo
                {
                    Arguments = $"/C start \"\" \"devenv\" \"{solutionFileFullPath}\"",
                    FileName = Environment.GetEnvironmentVariable("ComSpec"),
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                if (!loadProjects)
                {
                    processStartInfo.Arguments += " /DoNotLoadProjects";
                }
            }
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = solutionFileFullPath,
                    UseShellExecute = true,
                };
            }

            try
            {
                Process process = new Process
                {
                    StartInfo = processStartInfo,
                };

                logger.LogMessageHigh("Opening Visual Studio solution...");
                logger.LogMessageLow("  FileName = {0}", processStartInfo.FileName);
                logger.LogMessageLow("  Arguments = {0}", processStartInfo.Arguments);
                logger.LogMessageLow("  UseShellExecute = {0}", processStartInfo.UseShellExecute);
                logger.LogMessageLow("  WindowStyle = {0}", processStartInfo.WindowStyle);

                if (!process.Start())
                {
                    logger.LogError("Failed to launch Visual Studio.");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to launch Visual Studio. {e.Message}");
            }
        }

        public static ProjectCollection LoadProjectsAndReferences(
            IDictionary<string, string> globalProperties,
            string toolsVersion,
            bool collectStats,
            string msbuildBinPath,
            string projectFullPath,
            IEnumerable<ITaskItem> projectReferences,
            ISlnGenLogger logger)
        {
            // Create a ProjectCollection for this thread
            ProjectCollection projectCollection = new ProjectCollection
            {
                DefaultToolsVersion = toolsVersion,
                DisableMarkDirty = true, // Not sure but hoping this improves load performance
            };

#if NETFRAMEWORK
            string msbuildExePath = Path.Combine(msbuildBinPath, "MSBuild.exe");
#else
            string msbuildExePath = Path.Combine(msbuildBinPath, "MSBuild.dll");
#endif

            IMSBuildProjectLoader projectLoader = MSBuildProjectLoaderFactory.Create(msbuildExePath, logger);

            MSBuildProjectLoader msBuildProjectLoader = projectLoader as MSBuildProjectLoader;

            if (msBuildProjectLoader != null)
            {
                msBuildProjectLoader.CollectStats = collectStats;
            }

            logger.LogMessageHigh("Loading project references...");

            projectLoader.LoadProjects(projectCollection, globalProperties, projectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] { projectFullPath }));

            logger.LogMessageNormal($"Loaded {projectCollection.LoadedProjects.Count} project(s)");

            if (collectStats && msBuildProjectLoader != null)
            {
                LogStatistics(msBuildProjectLoader, logger);
            }

            return projectCollection;
        }

        public static void LogStatistics(MSBuildProjectLoader projectLoader, ISlnGenLogger logger)
        {
            logger.LogMessageLow("SlnGen Project Evaluation Performance Summary:");

            foreach (KeyValuePair<string, TimeSpan> item in projectLoader.Statistics.ProjectLoadTimes.OrderByDescending(i => i.Value))
            {
                logger.LogMessageLow($"  {Math.Round(item.Value.TotalMilliseconds, 0)} ms  {item.Key}", MessageImportance.Low);
            }
        }

        public static IEnumerable<string> ParseList(string items)
        {
            if (string.IsNullOrWhiteSpace(items))
            {
                return Enumerable.Empty<string>();
            }

            return items.Split(Semicolon, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());
        }

        public static IEnumerable<KeyValuePair<string, string>> ParseProperties(string properties)
        {
            if (string.IsNullOrWhiteSpace(properties))
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            return ParseList(properties)
                .Select(i => i.Split(EqualsSign, 2, StringSplitOptions.RemoveEmptyEntries)) // Split by '='
                .Where(i => i.Length == 2 && !string.IsNullOrWhiteSpace(i[0]) && !string.IsNullOrWhiteSpace(i[1]))
                .Select(i => new KeyValuePair<string, string>(i.First().Trim(), i.Last().Trim()));
        }
    }
}