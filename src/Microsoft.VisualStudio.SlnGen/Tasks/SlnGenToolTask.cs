// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.SlnGen.Tasks
{
    /// <summary>
    /// Represents a tool task that execute SlnGen.
    /// </summary>
    public sealed class SlnGenToolTask : ToolTask
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

        private static readonly FileInfo ThisAssemblyFileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);

        private readonly Lazy<IDictionary<string, string>> _globalPropertiesLazy;

        private readonly Lazy<ProjectInstance> _projectInstanceLazy;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnGenToolTask"/> class.
        /// </summary>
        public SlnGenToolTask()
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
        /// Gets or sets a value indicating whether to launch SlnGen under the debugger.
        /// </summary>
        public bool Debug { get; set; }

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
        /// Gets or sets the path to the MSBuild directory.
        /// </summary>
        [Required]
        public string MSBuildBinPath { get; set; }

        /// <summary>
        /// Gets or sets the full path to the project being built.
        /// </summary>
        [Required]
        public string ProjectFullPath { get; set; }

        /// <inheritdoc />
        protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

        /// <inheritdoc />
        protected override string ToolName => "slngen";

        /// <inheritdoc />
        public override bool Execute()
        {
            Dictionary<string, string> environmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process).Cast<DictionaryEntry>().OrderBy(i => (string)i.Key).ToDictionary(i => (string)i.Key, i => (string)i.Value, StringComparer.OrdinalIgnoreCase);

            environmentVariables.TryGetValue("PATH", out string path);

            environmentVariables["PATH"] = $"{MSBuildBinPath}{Path.PathSeparator}{path ?? string.Empty}";

            EnvironmentVariables = environmentVariables.Select(i => $"{i.Key}={i.Value}").ToArray();

            return base.Execute();
        }

        /// <inheritdoc />
        protected override string GenerateCommandLineCommands()
        {
            IDictionary<string, string> globalProperties = GetGlobalProperties();

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();

#if !NETFRAMEWORK
            commandLineBuilder.AppendFileNameIfNotNull(ThisAssemblyFileInfo.FullName);
#endif
            commandLineBuilder.AppendSwitch("--nologo");
            commandLineBuilder.AppendSwitch("--verbosity:Normal");
            commandLineBuilder.AppendSwitch("--consolelogger:NoSummary;ForceNoAlign");
            commandLineBuilder.AppendSwitchIfNotNull("--devenvfullpath:", GetPropertyValue(MSBuildPropertyNames.SlnGenDevEnvFullPath));
            commandLineBuilder.AppendSwitchIfNotNull("--folders:", GetPropertyValue(MSBuildPropertyNames.SlnGenFolders));
            commandLineBuilder.AppendSwitchIfNotNull("--launch:", GetPropertyValue(MSBuildPropertyNames.SlnGenLaunchVisualStudio));
            commandLineBuilder.AppendSwitchIfNotNull("--loadprojects:", GetPropertyValue(MSBuildPropertyNames.SlnGenLoadProjects));
            commandLineBuilder.AppendSwitchIfNotNull("--solutionfile:", GetPropertyValue(MSBuildPropertyNames.SlnGenSolutionFileFullPath));
            commandLineBuilder.AppendSwitchIfNotNull("--useshellexecute:", GetPropertyValue(MSBuildPropertyNames.SlnGenUseShellExecute));
            commandLineBuilder.AppendSwitchIfNotNull("--property:", globalProperties.Count == 0 ? null : string.Join(";", globalProperties.Select(i => $"{i.Key}={i.Value}")));

            if (string.Equals(GetPropertyValue(MSBuildPropertyNames.SlnGenDebug), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                commandLineBuilder.AppendSwitch("--debug");
            }

            if (string.Equals(GetPropertyValue(MSBuildPropertyNames.SlnGenBinLog), bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                commandLineBuilder.AppendSwitch("--binarylogger");
            }

            commandLineBuilder.AppendFileNameIfNotNull(ProjectFullPath);

            return commandLineBuilder.ToString();
        }

#if NETFRAMEWORK

        /// <inheritdoc />
        protected override string GenerateFullPathToTool() => ThisAssemblyFileInfo.FullName;

#else
        /// <inheritdoc />
        protected override string GenerateFullPathToTool() => "dotnet";
#endif

        /// <inheritdoc />
        protected override bool ValidateParameters()
        {
            if (BuildingSolutionFile)
            {
                Log.LogError("You must specify the path of a project or directory containing just a project in order to generate a solution.");

                return false;
            }

            return base.ValidateParameters();
        }

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