// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// An MSBuild task that generates a Visual Studio solution file.
    /// </summary>
    public class SlnGen : Task
    {
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
        /// Gets or sets a value indicating whether folders should be created.
        /// </summary>
        public bool Folders { get; set; }

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

        /// <inheritdoc cref="Task.Execute()" />
        public override bool Execute()
        {
            ISlnGenLogger logger = new TaskLogger(BuildEngine);

            if (BuildingSolutionFile)
            {
                if (!File.Exists(SolutionFileFullPath))
                {
                    Log.LogError($"Could not find part of the path '{SolutionFileFullPath}'.");
                }
            }
            else
            {
                IDictionary<string, string> globalProperties = GetGlobalProperties();

                // Load up the full project closure
                ProjectCollection projectCollection = SlnGenUtility.LoadProjectsAndReferences(
                    globalProperties,
                    ToolsVersion,
                    BuildEngine,
                    CollectStats,
                    ProjectFullPath,
                    ProjectReferences,
                    logger);

                // Return if loading projects logged any errors
                if (!Log.HasLoggedErrors)
                {
                    SlnGenUtility.GenerateSolutionFile(
                        projectCollection,
                        SolutionFileFullPath,
                        ProjectFullPath,
                        SlnProject.GetCustomProjectTypeGuids(CustomProjectTypeGuids),
                        Folders,
                        GetSolutionItems(),
                        logger);
                }
            }

            if (!Log.HasLoggedErrors && ShouldLaunchVisualStudio)
            {
                SlnGenUtility.LaunchVisualStudio(
                    DevEnvFullPath,
                    UseShellExecute,
                    SolutionFileFullPath,
                    logger);
            }

            return !Log.HasLoggedErrors;
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
            foreach (string solutionItem in SolutionItems.Select(i => i.GetMetadata("FullPath")).Where(i => !string.IsNullOrWhiteSpace(i)))
            {
                if (!fileExists(solutionItem))
                {
                    Log.LogMessageFromText($"The solution item \"{solutionItem}\" does not exist and will not be added to the solution.", MessageImportance.Low);
                }
                else
                {
                    yield return solutionItem;
                }
            }
        }

        private IDictionary<string, string> GetGlobalProperties()
        {
            IDictionary<string, string> globalProperties;

            if (InheritGlobalProperties && BuildEngine.TryGetProjectInstance(out ProjectInstance projectInstance))
            {
                // Clone the properties since they will be modified
                globalProperties = new Dictionary<string, string>(projectInstance.GlobalProperties, StringComparer.OrdinalIgnoreCase);

                foreach (string propertyToRemove in SlnGenUtility.ParseList(GlobalPropertiesToRemove).Where(i => globalProperties.ContainsKey(i)))
                {
                    globalProperties.Remove(propertyToRemove);
                }
            }
            else
            {
                globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (KeyValuePair<string, string> globalProperty in SlnGenUtility.ParseProperties(GlobalProperties))
            {
                globalProperties[globalProperty.Key] = globalProperty.Value;
            }

            if (globalProperties.Count > 0)
            {
                Log.LogMessageFromText("Global Properties:", MessageImportance.Low);
                foreach (KeyValuePair<string, string> globalProperty in globalProperties)
                {
                    Log.LogMessage(MessageImportance.Low, "  {0} = {1}", globalProperty.Key, globalProperty.Value);
                }
            }

            return globalProperties;
        }
    }
}