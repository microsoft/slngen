﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a project in a Visual Studio solution file.
    /// </summary>
    public sealed class SlnProject
    {
        /// <summary>
        /// Represents the default project type GUID for projects that load in the legacy project system.
        /// </summary>
        public static readonly Guid DefaultLegacyProjectTypeGuid = new Guid(VisualStudioProjectTypeGuids.LegacyCSharpProject);

        /// <summary>
        /// Represents the default project type GUID for projects that load in the NetSdk project system.
        /// </summary>
        public static readonly Guid DefaultNetSdkProjectTypeGuid = new Guid(VisualStudioProjectTypeGuids.NetSdkCSharpProject);

        /// <summary>
        /// Known project type GUIDs for legacy projects.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownLegacyProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = DefaultLegacyProjectTypeGuid,
            [ProjectFileExtensions.CSharp] = DefaultLegacyProjectTypeGuid,
            [ProjectFileExtensions.VisualBasic] = new Guid(VisualStudioProjectTypeGuids.LegacyVisualBasicProject),
        };

        /// <summary>
        /// Known project type GUIDs for .NET SDK projects.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownNetSdkProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = DefaultNetSdkProjectTypeGuid,
            [ProjectFileExtensions.CSharp] = DefaultNetSdkProjectTypeGuid,
            [ProjectFileExtensions.VisualBasic] = new Guid(VisualStudioProjectTypeGuids.NetSdkVisualBasicProject),
        };

        /// <summary>
        /// Known project type GUIDs for all project types.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [ProjectFileExtensions.AzureSdk] = new Guid(VisualStudioProjectTypeGuids.AzureSdk),
            [ProjectFileExtensions.AzureServiceFabric] = new Guid(VisualStudioProjectTypeGuids.AzureServiceFabric),
            [ProjectFileExtensions.Cpp] = new Guid(VisualStudioProjectTypeGuids.Cpp),
            [ProjectFileExtensions.FSharp] = new Guid(VisualStudioProjectTypeGuids.FSharp),
            [ProjectFileExtensions.JSharp] = new Guid(VisualStudioProjectTypeGuids.JSharp),
            [ProjectFileExtensions.LegacyCpp] = new Guid(VisualStudioProjectTypeGuids.Cpp),
            [ProjectFileExtensions.Native] = new Guid(VisualStudioProjectTypeGuids.Cpp),
            [ProjectFileExtensions.NuProj] = new Guid(VisualStudioProjectTypeGuids.NuProj),
            [ProjectFileExtensions.Scope] = new Guid(VisualStudioProjectTypeGuids.ScopeProject),
            [ProjectFileExtensions.Wix] = new Guid(VisualStudioProjectTypeGuids.Wix),
        };

        /// <summary>
        /// Gets or sets a list of Configuration values for this project.
        /// </summary>
        public IReadOnlyList<string> Configurations { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the full path to the project file.
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project is buildable in Visual Studio.
        /// </summary>
        public bool IsBuildable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project is deployable in Visual Studio.
        /// </summary>
        public bool IsDeployable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the main project in the Visual Studio solution.
        /// </summary>
        public bool IsMainProject { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a list of Platform values for this project.
        /// </summary>
        public IReadOnlyList<string> Platforms { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets a GUID representing the project.
        /// </summary>
        public Guid ProjectGuid { get; set; }

        /// <summary>
        /// Gets or sets a GUID representing the project type.
        /// </summary>
        public Guid ProjectTypeGuid { get; set; }

        /// <summary>
        /// Creates a solution project from the specified MSBuild project.
        /// </summary>
        /// <param name="project">The <see cref="Project " /> from MSBuild to create a solution project for.</param>
        /// <param name="customProjectTypeGuids"><see cref="IReadOnlyDictionary{String,Guid}" /> containing custom project type GUIDs.</param>
        /// <param name="isMainProject">Indicates whether this project is the main project in the solution.</param>
        /// <returns>A <see cref="SlnProject" /> for the specified MSBuild project.</returns>
        public static SlnProject FromProject(Project project, IReadOnlyDictionary<string, Guid> customProjectTypeGuids, bool isMainProject = false)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (customProjectTypeGuids == null)
            {
                throw new ArgumentNullException(nameof(customProjectTypeGuids));
            }

            if (!ShouldIncludeInSolution(project))
            {
                return null;
            }

            string name = project.GetPropertyValueOrDefault(MSBuildPropertyNames.AssemblyName, Path.GetFileNameWithoutExtension(project.FullPath));

            bool isUsingMicrosoftNETSdk = project.IsPropertyValueTrue(MSBuildPropertyNames.UsingMicrosoftNETSdk);

            string projectFileExtension = Path.GetExtension(project.FullPath);

            return new SlnProject
            {
                FullPath = project.FullPath,
                Name = name,
                ProjectGuid = GetProjectGuid(project, isUsingMicrosoftNETSdk),
                ProjectTypeGuid = GetProjectTypeGuid(projectFileExtension, isUsingMicrosoftNETSdk, customProjectTypeGuids),
                IsDeployable = GetIsDeployable(project, projectFileExtension),
                IsMainProject = isMainProject,
                Configurations = GetConfigurations(project, projectFileExtension, isUsingMicrosoftNETSdk),
                Platforms = GetPlatforms(project, projectFileExtension, isUsingMicrosoftNETSdk),
            };
        }

        internal static IReadOnlyList<string> GetConfigurations(Project project, string projectFileExtension, in bool isUsingMicrosoftNETSdk)
        {
            if (isUsingMicrosoftNETSdk)
            {
                string value = project.GetPropertyValue("Configurations");

                if (!value.IsNullOrWhiteSpace())
                {
                    return value.SplitSemicolonDelimitedList().ToList();
                }
            }

            return project.GetPossiblePropertyValuesOrDefault("Configuration", "Debug").ToList();
        }

        /// <summary>
        /// Parses custom project type GUIDs from the specified project.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to get custom project type GUIDs from.</param>
        /// <returns>An <see cref="IReadOnlyDictionary{String,Guid}" /> containing custom project type GUIDs by file extension.</returns>
        internal static IReadOnlyDictionary<string, Guid> GetCustomProjectTypeGuids(Project project)
        {
            Dictionary<string, Guid> projectTypeGuids = new Dictionary<string, Guid>();

            foreach (ProjectItem taskItem in project.GetItems(MSBuildItemNames.SlnGenCustomProjectTypeGuid))
            {
                string extension = taskItem.EvaluatedInclude.Trim();

                // Only consider items that start with a "." because they are supposed to be file extensions
                if (!extension.StartsWith("."))
                {
                    continue;
                }

                string projectTypeGuidString = taskItem.GetMetadataValue(MSBuildPropertyNames.ProjectTypeGuid).Trim();

                if (!projectTypeGuidString.IsNullOrWhiteSpace() && Guid.TryParse(projectTypeGuidString, out Guid projectTypeGuid))
                {
                    // Trim and ToLower the file extension
                    projectTypeGuids[taskItem.EvaluatedInclude.Trim().ToLowerInvariant()] = projectTypeGuid;
                }
            }

            return projectTypeGuids;
        }

        /// <summary>
        /// Gets a value indicating whether the project is deployable in Visual Studio.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to determine the deployablilty for.</param>
        /// <param name="projectFileExtension">The file extension of the specified project.</param>
        /// <returns>True if the specified project is deployable in Visual Studio, otherwise false.</returns>
        internal static bool GetIsDeployable(Project project, string projectFileExtension)
        {
            string isDeployableValue = project.GetPropertyValue(MSBuildPropertyNames.SlnGenIsDeployable);

            return isDeployableValue.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)
                   || (isDeployableValue.IsNullOrWhiteSpace() && string.Equals(projectFileExtension, ProjectFileExtensions.AzureServiceFabric, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the GUID for the specified project.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to get the project GUID for.</param>
        /// <param name="isUsingMicrosoftNETSdk">Indicates whether the specified project is using Microsoft.NET.Sdk.</param>
        /// <returns>The GUID of the specified project.</returns>
        internal static Guid GetProjectGuid(Project project, bool isUsingMicrosoftNETSdk)
        {
            Guid projectGuid = Guid.NewGuid();

            if (!isUsingMicrosoftNETSdk && !Guid.TryParse(project.GetPropertyValueOrDefault(MSBuildPropertyNames.ProjectGuid, projectGuid.ToString()), out projectGuid))
            {
                string projectGuidValue = project.GetPropertyValueOrDefault(MSBuildPropertyNames.ProjectGuid, projectGuid.ToString());

                if (!Guid.TryParse(projectGuidValue, out projectGuid))
                {
                    throw new InvalidProjectFileException(
                        projectFile: project.FullPath,
                        lineNumber: 0,
                        columnNumber: 0,
                        endLineNumber: 0,
                        endColumnNumber: 0,
                        message: $"The {MSBuildPropertyNames.ProjectGuid} property value \"{projectGuid}\" is not a valid GUID.",
                        errorSubcategory: null,
                        errorCode: null,
                        helpKeyword: null);
                }
            }

            return projectGuid;
        }

        /// <summary>
        /// Determines the project type GUID for the specified file project.
        /// </summary>
        /// <param name="projectFileExtension">The file extension of the specified project.</param>
        /// <param name="isUsingMicrosoftNETSdk">A value indicating whether or not the specified project is using Microsoft.NET.Sdk.</param>
        /// <param name="customProjectTypeGuids">An <see cref="IReadOnlyDictionary{String, Guid}" /> containing any user specified project type GUIDs to use.</param>
        /// <returns>A <see cref="Guid" /> representing the project type of the specified project.</returns>
        internal static Guid GetProjectTypeGuid(string projectFileExtension, bool isUsingMicrosoftNETSdk, IReadOnlyDictionary<string, Guid> customProjectTypeGuids)
        {
            if (customProjectTypeGuids.TryGetValue(projectFileExtension, out Guid projectTypeGuid) || KnownProjectTypeGuids.TryGetValue(projectFileExtension, out projectTypeGuid))
            {
                return projectTypeGuid;
            }

            if (isUsingMicrosoftNETSdk)
            {
                if (!KnownNetSdkProjectTypeGuids.TryGetValue(projectFileExtension, out projectTypeGuid))
                {
                    projectTypeGuid = DefaultNetSdkProjectTypeGuid;
                }

                return projectTypeGuid;
            }

            if (!KnownLegacyProjectTypeGuids.TryGetValue(projectFileExtension, out projectTypeGuid))
            {
                projectTypeGuid = DefaultLegacyProjectTypeGuid;
            }

            return projectTypeGuid;
        }

        /// <summary>
        /// Gets the solution items' full paths.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> containing the solution items.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <param name="fileExists">A <see cref="Func{String, Boolean}"/> to use when determining if a file exists.</param>
        /// <exception cref="ArgumentNullException"><paramref name="project" /> is null
        /// -or-
        /// <paramref name="logger" /> is null
        /// -or-
        /// <paramref name="fileExists" /> is null.</exception>
        /// <returns>An <see cref="IEnumerable{String}"/> of full paths to include as solution items.</returns>
        internal static IEnumerable<string> GetSolutionItems(Project project, ISlnGenLogger logger, Func<string, bool> fileExists = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (fileExists == null)
            {
                fileExists = File.Exists;
            }

            foreach (string solutionItem in project.GetItems(MSBuildItemNames.SlnGenSolutionItem).Select(i => i.GetMetadataValue("FullPath")).Where(i => !i.IsNullOrWhiteSpace()))
            {
                if (!fileExists(solutionItem))
                {
                    logger.LogMessageLow($"The solution item \"{solutionItem}\" does not exist and will not be added to the solution.", MessageImportance.Low);
                }
                else
                {
                    yield return solutionItem;
                }
            }
        }

        /// <summary>
        /// Checks whether a project should be included in the solution or not.
        /// </summary>
        /// <param name="project">The project.</param>
        /// <returns><code>true</code> if it should be included, false otherwise.</returns>
        internal static bool ShouldIncludeInSolution(Project project)
        {
            return
                !project.GetPropertyValue(MSBuildPropertyNames.IncludeInSolutionFile).Equals(bool.FalseString, StringComparison.OrdinalIgnoreCase) // Filter out projects that explicitly should not be included
                &&
                !project.GetPropertyValue(MSBuildPropertyNames.IsTraversal).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) // Filter out traversal projects by looking for an IsTraversal property
                &&
                !project.GetPropertyValue(MSBuildPropertyNames.IsTraversalProject).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);  // Filter out traversal projects by looking for an IsTraversal property
        }

        private static IReadOnlyList<string> GetPlatforms(Project project, string projectFileExtension, bool isUsingMicrosoftNETSdk)
        {
            if (isUsingMicrosoftNETSdk)
            {
                string value = project.GetPropertyValue("Platforms");

                if (!value.IsNullOrWhiteSpace())
                {
                    return value.SplitSemicolonDelimitedList().ToList();
                }
            }

            return project.GetPossiblePropertyValuesOrDefault("Platform", "Any CPU").ToList();
        }

        private static IEnumerable<string> GetPlatforms(Project project)
        {
            string platforms = project.GetPropertyValue("Platforms");

            if (!platforms.IsNullOrWhiteSpace())
            {
                foreach (string value in platforms.Split(';'))
                {
                    if (string.Equals(value, "AnyCPU", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return "Any CPU";
                    }
                    else
                    {
                        yield return value;
                    }
                }
            }
            else
            {
                foreach (string platform in project.GetPossiblePropertyValuesOrDefault("Platform", "Any CPU"))
                {
                    if (string.Equals(platform, "AnyCPU", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return "Any CPU";
                    }
                    else
                    {
                        yield return platform;
                    }
                }
            }
        }
    }
}