using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
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
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            // Get the existing ProjectInstance from MSBuild
            BuildEngine.TryGetProjectInstance(out ProjectInstance projectInstance);

            // Set no global properties if ProjectInstance is null for whatever reason
            IDictionary<string, string> globalProperties = projectInstance?.GlobalProperties ?? new Dictionary<string, string>();

            // Load up the full project closure
            ProjectCollection projectCollection = LoadProjectsAndReferences(globalProperties);

            // Return if loading projects logged any errors
            if (HasLoggedErrors)
            {
                return;
            }

            GenerateSolutionFile(projectCollection.LoadedProjects);

            LaunchVisualStudio();
        }

        private void GenerateSolutionFile(ICollection<Project> projects)
        {
            if (String.IsNullOrWhiteSpace(SolutionFileFullPath))
            {
                SolutionFileFullPath = Path.ChangeExtension(ProjectFullPath, ".sln");
            }

            LogMessage($"Generating Visual Studio solution \"{SolutionFileFullPath}\"...", MessageImportance.High);

            SolutionFile solution = new SolutionFile(projects.Where(ShouldIncludeInSolution).Select(p => new SolutionProject(p, p.FullPath == ProjectFullPath)));

            File.WriteAllText(SolutionFileFullPath, solution.ToString());
        }

        private void LaunchVisualStudio()
        {
            if (!ShouldLaunchVisualStudio)
            {
                return;
            }

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
            else
            {
                processStartInfo = new ProcessStartInfo
                {
                    //Arguments = $"/C start devenv.exe {solutionPath}",
                    FileName = SolutionFileFullPath,
                    //WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };
            }

            try
            {
                LogMessage($"{processStartInfo.FileName} {processStartInfo.Arguments}");

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

            LogMessage("Loading project references...", MessageImportance.High);

            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(ProjectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] {ProjectFullPath}));

            LogMessage($"Loaded {projectCollection.LoadedProjects.Count} project(s)");

            if (CollectStats)
            {
                LogStatistics(projectLoader);
            }

            return projectCollection;
        }

        private void LogStatistics(MSBuildProjectLoader projectLoader)
        {
            LogMessage("SlnGen Project Evaluation Performance Summary:", MessageImportance.Low);

            foreach (KeyValuePair<string, TimeSpan> item in projectLoader.Statistics.ProjectLoadTimes.OrderByDescending(i => i.Value))
            {
                // String.Format(CultureInfo.CurrentCulture, "{0,5}", ;
                LogMessage($"  {Math.Round(item.Value.TotalMilliseconds, 0),5}  {item.Key}", MessageImportance.Low);
            }
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