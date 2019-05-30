// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using JetBrains.Annotations;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// An MSBuild task that generates a Visual Studio solution file.
    /// </summary>
    public class SlnGen : TaskBase
    {
        internal const string CustomProjectTypeGuidMetadataName = "ProjectTypeGuid";

        /// <summary>
        /// Gets or sets a value indicating whether MSBuild is currently building a Visual Studio solution file.
        /// </summary>
        public bool BuildingSolutionFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether statistics should be collected when loading projects.
        /// </summary>
        public bool CollectStats { get; set; }

        /// <summary>
        /// Gets or sets a list of custom project type GUIDs.
        /// </summary>
        public ITaskItem[] CustomProjectTypeGuids { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Gets or sets an optional full path to Visual Studio's devenv.exe to use when opening the solution file.
        /// </summary>
        public string DevEnvFullPath { get; set; }

        /// <summary>
        /// Gets or sets a value containing global properties to set when evaluation projects.
        /// </summary>
        public string GlobalProperties { get; set; }

        /// <summary>
        /// Gets or sets a list of global properties to remove.
        /// </summary>
        public string GlobalPropertiesToRemove { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether global properties should be inherited from the currently executing project.
        /// </summary>
        public bool InheritGlobalProperties { get; set; }

        /// <summary>
        /// Gets or sets the full path to the project being built.
        /// </summary>
        [Required]
        public string ProjectFullPath { get; set; }

        /// <summary>
        /// Gets or sets the project references of the current project.
        /// </summary>
        public ITaskItem[] ProjectReferences { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Gets or sets a value indicating whether Visual Studio should be launched after generating the solution file.
        /// </summary>
        public bool ShouldLaunchVisualStudio { get; set; }

        /// <summary>
        /// Gets or sets the full path to the solution file to generate.  If a value is not specified, a path is derived from the <see cref="ProjectFullPath"/>.
        /// </summary>
        public string SolutionFileFullPath { get; set; }

        /// <summary>
        /// Gets or sets a list of items to be shown in the Visual Studio solution file.
        /// </summary>
        public ITaskItem[] SolutionItems { get; set; } = new ITaskItem[0];

        /// <summary>
        /// Gets or sets the tools version of the project.
        /// </summary>
        [Required]
        public string ToolsVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Visual Studio should be launched by telling the shell to open whatever program is registered to handle .sln files.
        /// </summary>
        public bool UseShellExecute { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether folders should be created.
        /// </summary>
        public bool Folders { get; set; }

        /// <summary>
        /// Checks whether a project should be included in the solution or not.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns><c> true </c> if it should be included, <c> false </c> otherwise.</returns>
        internal static bool ShouldIncludeInSolution(Project project)
        {
            return
                !project.GetPropertyValue("IncludeInSolutionFile").Equals("false", StringComparison.OrdinalIgnoreCase) // Filter out projects that explicitly should not be included
                &&
                !project.GetPropertyValue("IsTraversal").Equals("true", StringComparison.OrdinalIgnoreCase);  // Filter out traversal projects by looking for an IsTraversal property
        }

        /// <summary>
        /// Gets the solution items' full paths.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{String}"/> of full paths to include as solution items.</returns>
        internal IEnumerable<string> GetSolutionItems()
        {
            return GetSolutionItems(File.Exists);
        }

        /// <summary>
        /// Gets the solution items' full paths.
        /// </summary>
        /// <param name="fileExists">A <see cref="Func{String, Boolean}"/> to use when determining if a file exists.</param>
        /// <returns>An <see cref="IEnumerable{String}"/> of full paths to include as solution items.</returns>
        internal IEnumerable<string> GetSolutionItems(Func<string, bool> fileExists)
        {
            foreach (string solutionItem in SolutionItems.Select(i => i.GetMetadata("FullPath")).Where(i => !String.IsNullOrWhiteSpace(i)))
            {
                if (!fileExists(solutionItem))
                {
                    LogMessageLow($"The solution item \"{solutionItem}\" does not exist and will not be added to the solution.");
                }
                else
                {
                    yield return solutionItem;
                }
            }
        }

        [NotNull]
        internal Dictionary<string, Guid> ParseCustomProjectTypeGuids()
        {
            Dictionary<string, Guid> projectTypeGuids = new Dictionary<string, Guid>();

            foreach (ITaskItem taskItem in CustomProjectTypeGuids)
            {
                string extension = taskItem.ItemSpec.Trim();

                // Only consider items that start with a "." because they are supposed to be file extensions
                if (!extension.StartsWith("."))
                {
                    continue;
                }

                string projectTypeGuidString = taskItem.GetMetadata(CustomProjectTypeGuidMetadataName)?.Trim();

                if (!String.IsNullOrWhiteSpace(projectTypeGuidString) && Guid.TryParse(projectTypeGuidString, out Guid projectTypeGuid))
                {
                    // Trim and ToLower the file extension
                    projectTypeGuids[taskItem.ItemSpec.Trim().ToLowerInvariant()] = projectTypeGuid;
                }
            }

            return projectTypeGuids;
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            if (BuildingSolutionFile)
            {
                if (!File.Exists(SolutionFileFullPath))
                {
                    LogError($"Could not find part of the path '{SolutionFileFullPath}'.");
                    return;
                }
            }
            else
            {
                IDictionary<string, string> globalProperties = GetGlobalProperties();

                // Load up the full project closure
                ProjectCollection projectCollection = LoadProjectsAndReferences(globalProperties);

                // Return if loading projects logged any errors
                if (!HasLoggedErrors)
                {
                    GenerateSolutionFile(projectCollection.LoadedProjects);
                }
            }

            if (!HasLoggedErrors && ShouldLaunchVisualStudio)
            {
                LaunchVisualStudio();
            }
        }

        private void GenerateSolutionFile(ICollection<Project> projects)
        {
            if (String.IsNullOrWhiteSpace(SolutionFileFullPath))
            {
                SolutionFileFullPath = Path.ChangeExtension(ProjectFullPath, ".sln");
            }

            Dictionary<string, Guid> customProjectTypeGuids = ParseCustomProjectTypeGuids();

            LogMessageHigh($"Generating Visual Studio solution \"{SolutionFileFullPath}\" ...");

            if (customProjectTypeGuids.Count > 0)
            {
                LogMessageLow("Custom Project Type GUIDs:");
                foreach (KeyValuePair<string, Guid> item in customProjectTypeGuids)
                {
                    LogMessageLow("  {0} = {1}", item.Key, item.Value);
                }
            }

            SlnFile solution = new SlnFile();

            solution.AddProjects(
                projects.Where(ShouldIncludeInSolution)
                        .Select(p => SlnProject.FromProject(p, customProjectTypeGuids, p.FullPath == ProjectFullPath)));

            solution.AddSolutionItems(GetSolutionItems());

            solution.Save(SolutionFileFullPath, Folders);
        }

        private IDictionary<string, string> GetGlobalProperties()
        {
            IDictionary<string, string> globalProperties;

            if (InheritGlobalProperties && BuildEngine.TryGetProjectInstance(out ProjectInstance projectInstance))
            {
                // Clone the properties since they will be modified
                globalProperties = new Dictionary<string, string>(projectInstance.GlobalProperties, StringComparer.OrdinalIgnoreCase);

                foreach (string propertyToRemove in ParseList(GlobalPropertiesToRemove).Where(i => globalProperties.ContainsKey(i)))
                {
                    globalProperties.Remove(propertyToRemove);
                }
            }
            else
            {
                globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (KeyValuePair<string, string> globalProperty in ParseProperties(GlobalProperties))
            {
                globalProperties[globalProperty.Key] = globalProperty.Value;
            }

            if (globalProperties.Count > 0)
            {
                LogMessageLow("Global Properties:");
                foreach (KeyValuePair<string, string> globalProperty in globalProperties)
                {
                    LogMessageLow("  {0} = {1}", globalProperty.Key, globalProperty.Value);
                }
            }

            return globalProperties;
        }

        private void LaunchVisualStudio()
        {
            ProcessStartInfo processStartInfo;

            if (!String.IsNullOrWhiteSpace(DevEnvFullPath))
            {
                if (!File.Exists(DevEnvFullPath))
                {
                    LogError($"The specified path to Visual Studio ({DevEnvFullPath}) does not exist or is inaccessible.");

                    return;
                }

                processStartInfo = new ProcessStartInfo
                {
                    FileName = DevEnvFullPath,
                    Arguments = $"\"{SolutionFileFullPath}\"",
                };
            }
            else if (!UseShellExecute)
            {
                processStartInfo = new ProcessStartInfo
                {
                    Arguments = $"/C start \"\" \"devenv.exe\" \"{SolutionFileFullPath}\"",
                    FileName = SolutionFileFullPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
            }
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    FileName = SolutionFileFullPath,
                    UseShellExecute = true,
                };
            }

            try
            {
                Process process = new Process
                {
                    StartInfo = processStartInfo,
                };

                LogMessageHigh("Opening Visual Studio solution...");
                LogMessageLow("  FileName = {0}", processStartInfo.FileName);
                LogMessageLow("  Arguments = {0}", processStartInfo.Arguments);
                LogMessageLow("  UseShellExecute = {0}", processStartInfo.UseShellExecute);
                LogMessageLow("  WindowStyle = {0}", processStartInfo.WindowStyle);

                if (!process.Start())
                {
                    LogError("Failed to launch Visual Studio.");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to launch Visual Studio. {e.Message}");
            }
        }

        private ProjectCollection LoadProjectsAndReferences(IDictionary<string, string> globalProperties)
        {
            // Create an MSBuildProject loader with the same global properties of the project that requested a solution file
            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(globalProperties, ToolsVersion, BuildEngine, ProjectLoadSettings.IgnoreMissingImports)
            {
                CollectStats = CollectStats,
            };

            LogMessageHigh("Loading project references...");

            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(ProjectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] { ProjectFullPath }));

            LogMessageNormal($"Loaded {projectCollection.LoadedProjects.Count} project(s)");

            if (CollectStats)
            {
                LogStatistics(projectLoader);
            }

            return projectCollection;
        }

        private void LogStatistics(MSBuildProjectLoader projectLoader)
        {
            LogMessageLow("SlnGen Project Evaluation Performance Summary:");

            foreach (KeyValuePair<string, TimeSpan> item in projectLoader.Statistics.ProjectLoadTimes.OrderByDescending(i => i.Value))
            {
                LogMessageLow($"  {Math.Round(item.Value.TotalMilliseconds, 0)} ms  {item.Key}", MessageImportance.Low);
            }
        }

        private IEnumerable<string> ParseList(string items)
        {
            if (String.IsNullOrWhiteSpace(items))
            {
                return Enumerable.Empty<string>();
            }

            char[] itemSeparators = { ';' };

            // Split by ';'
            return items.Split(itemSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim()) // Trim each entry
                .Where(i => !String.IsNullOrWhiteSpace(i)); // Ignore empty entries after trimming
        }

        private IEnumerable<KeyValuePair<string, string>> ParseProperties(string properties)
        {
            if (String.IsNullOrWhiteSpace(properties))
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            char[] propertySeparators = { '=' };

            // Split by ';'
            return ParseList(properties)
                .Select(i => i.Split(propertySeparators, 2, StringSplitOptions.RemoveEmptyEntries)) // Split by '='
                .Where(i => i.Length == 2) // Ignore entries that don't have two items
                .Select(i => new KeyValuePair<string, string>(i.First().Trim(), i.Last().Trim())) // Create a KeyValuePair with trimmed Key and Value
                .Where(i => !String.IsNullOrWhiteSpace(i.Key) && !String.IsNullOrWhiteSpace(i.Value)); // Ignore items with an empty key or value
        }
    }
}