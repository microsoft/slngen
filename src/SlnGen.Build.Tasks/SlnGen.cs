namespace SlnGen.Build.Tasks
{
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using BuildTask;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;

    public class SlnGen : TaskBase
    {
        public ITaskItem[] ProjectReferences { get; set; }

        [Required]
        public string ProjectFullPath { get; set; }

        [Required]
        public string ToolsVersion { get; set; }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask()
        {
            var projectInstance = this.BuildEngine.GetProjectInstance();

            this.LogMessage("Loading project references...", MessageImportance.High);

            var projectLoader = new MSBuildProjectLoader(projectInstance.GlobalProperties, this.ToolsVersion, this.BuildEngine, ProjectLoadSettings.IgnoreMissingImports);

            this.ProjectReferences = this.ProjectReferences ?? new ITaskItem[0];

            var projectPaths = this.ProjectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] { this.ProjectFullPath }).ToArray();
            var projectCollection = projectLoader.LoadProjectsAndReferences(projectPaths);

            this.LogMessage($"Loaded {projectCollection.LoadedProjects.Count} projects");

            this.LogMessage("Generating solution file...", MessageImportance.High);

            // filter out the traversal (.proj) files.
            var solution = new Solution(projectCollection.LoadedProjects.Where(e => !e.FullPath.EndsWith(".proj")).Select(p => new ProjectInfo(p, p.FullPath == this.ProjectFullPath)));

            var parent = Directory.GetParent(this.ProjectFullPath).FullName;
            var solutionPath = Path.Combine(parent, $"{Path.GetFileNameWithoutExtension(this.ProjectFullPath)}.sln");

            File.WriteAllText(solutionPath, solution.ToString());

            Process.Start(new ProcessStartInfo
            {
                FileName = @"cmd",
                Arguments = $"/C start devenv.exe {solutionPath}",
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}
