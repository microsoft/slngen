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

            ProjectInstance projectInstance = BuildEngine.GetProjectInstance();

            LogMessage("Loading project references...", MessageImportance.High);

            MSBuildProjectLoader projectLoader = new MSBuildProjectLoader(projectInstance.GlobalProperties, ToolsVersion, BuildEngine, ProjectLoadSettings.IgnoreMissingImports);

            ProjectCollection projectCollection = projectLoader.LoadProjectsAndReferences(ProjectReferences.Select(i => i.GetMetadata("FullPath")));

            LogMessage($"Loaded {projectCollection.LoadedProjects.Count} projects");

            LogMessage("Generating solution file...", MessageImportance.High);

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
