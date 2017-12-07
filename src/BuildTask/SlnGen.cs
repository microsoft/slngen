using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BuildTask;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace SlnGen.Build.Tasks
{
    using System.IO;

    public class SlnGen : TaskBase
    {
        public ITaskItem[] ProjectReferences { get; set; }

        [Required]
        public string ProjectFullPath { get; set; }

        [Required]
        public string ToolsVersion { get; set; }

        protected override void ExecuteTask()
        {
            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(500);
            //}

            var projectInstance = BuildEngine.GetProjectInstance();

            this.LogMessage("Loading project references...", MessageImportance.High);

            var projectLoader = new MSBuildProjectLoader(projectInstance.GlobalProperties, ToolsVersion, BuildEngine, ProjectLoadSettings.IgnoreMissingImports);

            var projectCollection = projectLoader.LoadProjectsAndReferences(ProjectReferences.Select(i => i.GetMetadata("FullPath")).Concat(new[] { this.ProjectFullPath }));

            this.LogMessage($"Loaded {projectCollection.LoadedProjects.Count} projects");

            this.LogMessage("Generating solution file...", MessageImportance.High);

            var solution = new Solution();
            foreach (var project in projectCollection.LoadedProjects)
            {
                solution.Projects.Add(project);
            }

            var parent = Directory.GetParent(this.ProjectFullPath).FullName;
            var solutionPath = Path.Combine(parent, $"{Path.GetFileNameWithoutExtension(this.ProjectFullPath)}.sln");

            File.WriteAllText(solutionPath, solution.ToString());


            Process.Start(new ProcessStartInfo
            {
                FileName = @"cmd",
                Arguments = $"/C start devenv.exe {solutionPath}",
                WindowStyle = ProcessWindowStyle.Hidden
            });

            //foreach (Project project in projectCollection.LoadedProjects)
            //{
            //    LogMessage($"  {project.FullPath}");
            //    LogMessage($"    Name        = {project.GetPropertyValue("ProjectName")}");
            //    LogMessage($"    ProjectGuid = {project.GetPropertyValue("ProjectGuid")}");
            //    LogMessage("");
            //}
        }
    }
}
