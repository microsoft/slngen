// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.SlnGen.Tasks
{
    public sealed class ExecuteSlnGen : ToolTask
    {
        /// <summary>
        /// Stores the <see cref="Assembly"/> containing the type <see cref="BuildManager"/>.
        /// </summary>
        private static readonly Lazy<Assembly> BuildManagerAssemblyLazy = new Lazy<Assembly>(() => typeof(BuildManager).Assembly);

        /// <summary>
        /// Stores the <see cref="PropertyInfo"/> for the <see cref="Microsoft.Build.BackEnd.BuildRequestConfiguration.Project"/> property.
        /// </summary>
        private static readonly Lazy<PropertyInfo> BuildRequestConfigurationProjectPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestConfigurationTypeLazy.Value.GetProperty("Project"));

        /// <summary>
        /// Stores the <see cref="Type"/> of <see cref="Microsoft.Build.BackEnd.BuildRequestConfiguration"/>.
        /// </summary>
        private static readonly Lazy<Type> BuildRequestConfigurationTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestConfiguration", throwOnError: false));

        /// <summary>
        /// Stores the <see cref="PropertyInfo"/> for the <see cref="Microsoft.Build.BackEnd.BuildRequestEntry.RequestConfiguration"/> property.
        /// </summary>
        private static readonly Lazy<PropertyInfo> BuildRequestEntryRequestConfigurationPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestEntryTypeLazy.Value.GetProperty("RequestConfiguration"));

        /// <summary>
        /// Stores the <see cref="Type"/> of <see cref="Microsoft.Build.BackEnd.BuildRequestEntry"/>.
        /// </summary>
        private static readonly Lazy<Type> BuildRequestEntryTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestEntry", throwOnError: false));

        private readonly Lazy<IDictionary<string, string>> _globalPropertiesLazy;

        private readonly Lazy<ProjectInstance> _projectInstanceLazy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteSlnGen"/> class.
        /// </summary>
        public ExecuteSlnGen()
            : base(Strings.ResourceManager)
        {
            _projectInstanceLazy = new Lazy<ProjectInstance>(GetProjectInstance);

            _globalPropertiesLazy = new Lazy<IDictionary<string, string>>(GetGlobalProperties);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not a Visual Studio solution file is being built.
        /// </summary>
        [Required]
        public bool BuildingSolutionFile { get; set; }

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
        /// The path to the directory containing MSBuild.exe.
        /// </summary>
        [Required]
        public string MSBuildBinPath { get; set; }

        /// <summary>
        /// Gets or sets the full path to the project being built.
        /// </summary>
        [Required]
        public string ProjectFullPath { get; set; }

        protected override string ToolName => "slngen";

        protected override string GenerateCommandLineCommands()
        {
            ISlnGenLogger logger = new TaskLogger(BuildEngine);

            /*
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
                    CollectStats,
                    MSBuildBinPath,
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
                        SlnProject.GetCustomProjectTypeGuids(CustomProjectTypeGuids.Select(i => new MSBuildTaskItem(i))),
                        Folders,
                        enableConfigurationAndPlatforms: true,
                        SlnFile.GetSolutionItems(SolutionItems.Select(i => new MSBuildTaskItem(i)), logger),
                        configurations: null,
                        platforms: null,
                        logger);
                }
            }

            if (!Log.HasLoggedErrors && ShouldLaunchVisualStudio)
            {
                SlnGenUtility.LaunchVisualStudio(
                    DevEnvFullPath,
                    UseShellExecute,
                    SolutionFileFullPath,
                    loadProjects: true,
                    logger);
            }
            */

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();

            commandLineBuilder.AppendSwitchIfNotNull("--devenvfullpath:", GetPropertyValue(MSBuildPropertyNames.SlnGenDevEnvFullPath));
            commandLineBuilder.AppendSwitchIfNotNull("--folders:", GetPropertyValue(MSBuildPropertyNames.SlnGenFolders));
            commandLineBuilder.AppendSwitchIfNotNull("--launch:", GetPropertyValue(MSBuildPropertyNames.SlnGenLaunchVisualStudio));
            commandLineBuilder.AppendSwitchIfNotNull("--loadprojects:", GetPropertyValue(MSBuildPropertyNames.SlnGenLoadProjects));
            commandLineBuilder.AppendSwitchIfNotNull("--solutionfile:", GetPropertyValue(MSBuildPropertyNames.SlnGenSolutionFileFullPath));
            commandLineBuilder.AppendSwitchIfNotNull("--useshellexecute:", GetPropertyValue(MSBuildPropertyNames.SlnGenUseShellExecute));

            commandLineBuilder.AppendFileNameIfNotNull(ProjectFullPath);

            return commandLineBuilder.ToString();
        }

        /// <inheritdoc />
        protected override string GenerateFullPathToTool() => Assembly.GetExecutingAssembly().Location;

        private IDictionary<string, string> GetGlobalProperties()
        {
            IDictionary<string, string> globalProperties;

            if (InheritGlobalProperties && _projectInstanceLazy.Value != null)
            {
                // Clone the properties since they will be modified
                globalProperties = new Dictionary<string, string>(_projectInstanceLazy.Value.GlobalProperties, StringComparer.OrdinalIgnoreCase);

                foreach (string propertyToRemove in GlobalPropertiesToRemove.SplitSemicolonDelimitedList().Where(i => globalProperties.ContainsKey(i)))
                {
                    globalProperties.Remove(propertyToRemove);
                }
            }
            else
            {
                globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (KeyValuePair<string, string> globalProperty in GlobalProperties.SplitProperties())
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

        /// <summary>
        /// Attempts to get the current <see cref="ProjectInstance"/> of the executing task via reflection.
        /// </summary>
        /// <returns>A <see cref="ProjectInstance"/> object if one could be determined, otherwise null..</returns>
        private ProjectInstance GetProjectInstance()
        {
            try
            {
                FieldInfo requestEntryFieldInfo = BuildEngine.GetType().GetField("_requestEntry", BindingFlags.Instance | BindingFlags.NonPublic);

                if (requestEntryFieldInfo != null && BuildRequestEntryTypeLazy.Value != null && BuildRequestConfigurationTypeLazy.Value != null)
                {
                    object requestEntry = requestEntryFieldInfo.GetValue(BuildEngine);

                    if (requestEntry != null && BuildRequestEntryRequestConfigurationPropertyInfo.Value != null)
                    {
                        object requestConfiguration = BuildRequestEntryRequestConfigurationPropertyInfo.Value.GetValue(requestEntry);

                        if (requestConfiguration != null && BuildRequestConfigurationProjectPropertyInfo.Value != null)
                        {
                            return BuildRequestConfigurationProjectPropertyInfo.Value.GetValue(requestConfiguration) as ProjectInstance;
                        }
                    }
                }
            }
            catch
            {
                // Ignored because we never want this method to throw since its using reflection to access internal members that could go away with any future release of MSBuild
            }

            return null;
        }

        private string GetPropertyValue(string name)
        {
            if (_globalPropertiesLazy.Value.TryGetValue(name, out string value))
            {
                return value.IsNullOrWhiteSpace() ? null : value;
            }

            if (_projectInstanceLazy.Value == null)
            {
                return null;
            }

            value = _projectInstanceLazy.Value.GetPropertyValue(name);

            return value.IsNullOrWhiteSpace() ? null : value;
        }
    }
}