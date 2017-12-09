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
    public class SlnGen : TaskBase
    {
        /// <summary>
        /// Gets or sets a value indicating if statistics should be collected when loading projects.
        /// </summary>
        public bool CollectStats { get; set; }

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
        /// Gets or sets a value indicating if global properties should be inherited from the currently executing project.
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
        /// Gets or sets a value indicating if Visual Studio should be launched after generating the solution file.
        /// </summary>
        public bool ShouldLaunchVisualStudio { get; set; }

        /// <summary>
        /// Gets or sets the full path to the solution file to generate.  If a value is not specified, a path is derived from the <see cref="ProjectFullPath"/>.
        /// </summary>
        public string SolutionFileFullPath { get; set; }

        /// <summary>
        /// Gets or sets the tools version of the project.
        /// </summary>
        [Required]
        public string ToolsVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if Visual Studio should be launched by telling the shell to open whatever program is registered to handle .sln files.
        /// </summary>
        public bool UseShellExecute { get; set; }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            IDictionary<string, string> globalProperties = GetGlobalProperties();

            // Load up the full project closure
            ProjectCollection projectCollection = LoadProjectsAndReferences(globalProperties);

            // Return if loading projects logged any errors
            if (!HasLoggedErrors)
            {
                GenerateSolutionFile(projectCollection.LoadedProjects);

                if (!HasLoggedErrors && ShouldLaunchVisualStudio)
                {
                    LaunchVisualStudio();
                }
            }
        }

        private void GenerateSolutionFile(ICollection<Project> projects)
        {
            if (String.IsNullOrWhiteSpace(SolutionFileFullPath))
            {
                SolutionFileFullPath = Path.ChangeExtension(ProjectFullPath, ".sln");
            }

            LogMessageHigh($"Generating Visual Studio solution \"{SolutionFileFullPath}\"...");

            SlnFile solution = new SlnFile(projects.Where(ShouldIncludeInSolution).Select(p => SlnProject.FromProject(p, p.FullPath == ProjectFullPath)));

            File.WriteAllText(SolutionFileFullPath, solution.ToString());
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
                    LogMessageLow($"{0} = {1}", MessageImportance.Low, globalProperty.Key, globalProperty.Value);
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
                    Arguments = $"\"{SolutionFileFullPath}\""
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
                    UseShellExecute = true
                };
            }

            try
            {
                LogMessageNormal($"{processStartInfo.FileName} {processStartInfo.Arguments}");

                Process process = new Process
                {
                    StartInfo = processStartInfo,
                };

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
                CollectStats = CollectStats
            };

            LogMessageHigh("Loading project references...");

            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(ProjectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] {ProjectFullPath}));

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
                // String.Format(CultureInfo.CurrentCulture, "{0,5}", ;
                LogMessageLow($"  {Math.Round(item.Value.TotalMilliseconds, 0),5}  {item.Key}", MessageImportance.Low);
            }
        }

        private IEnumerable<string> ParseList(string items)
        {
            char[] itemSeparators = {';'};

            // Split by ';'
            return items.Split(itemSeparators, StringSplitOptions.RemoveEmptyEntries)
                // Trim each entry
                .Select(i => i.Trim())
                // Ignore empty entries after trimming
                .Where(i => !String.IsNullOrWhiteSpace(i));
        }

        private IEnumerable<KeyValuePair<string, string>> ParseProperties(string properties)
        {
            char[] propertySeparators = {'='};

            // Split by ';'
            return ParseList(properties)
                // Split by '='
                .Select(i => i.Split(propertySeparators, 2, StringSplitOptions.RemoveEmptyEntries))
                // Ignore entries that don't have two items
                .Where(i => i.Length == 2)
                // Create a KeyValuePair with trimmed Key and Value
                .Select(i => new KeyValuePair<string, string>(i.First().Trim(), i.Last().Trim()))
                // Ignore items with an empty key or value
                .Where(i => !String.IsNullOrWhiteSpace(i.Key) && !String.IsNullOrWhiteSpace(i.Value));
        }

        private bool ShouldIncludeInSolution(Project project)
        {
            return
                // Filter out projects that explicitly should not be included
                !project.GetPropertyValue("IncludeInSolutionFile").Equals("false", StringComparison.OrdinalIgnoreCase)
                || // Filter out traversal projects by looking for an IsTraversal property
                !project.GetPropertyValue("IsTraversal").Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}