// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using JetBrains.Annotations;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace SlnGen.Build.Tasks.Internal
{
    internal sealed class SlnProject
    {
        public const string AssemblyNamePropertyName = "AssemblyName";
        public const string ProjectGuidPropertyName = "ProjectGuid";
        public const string UsingMicrosoftNetSdkPropertyName = "UsingMicrosoftNETSdk";
        public static readonly Guid DefaultLegacyProjectTypeGuid = new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        public static readonly Guid DefaultNetSdkProjectTypeGuid = new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556");

        /// <summary>
        /// Known project type GUIDs for legacy projects.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownLegacyProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = DefaultLegacyProjectTypeGuid,
            [".csproj"] = DefaultLegacyProjectTypeGuid,
            [".vbproj"] = new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"),
        };

        /// <summary>
        /// Known project type GUIDs for .NET SDK projects.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownNetSdkProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = DefaultNetSdkProjectTypeGuid,
            [".csproj"] = DefaultNetSdkProjectTypeGuid,
            [".vbproj"] = new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9"),
        };

        /// <summary>
        /// Known project type GUIDs for all project types.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Guid> KnownProjectTypeGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
        {
            [".ccproj"] = new Guid("151D2E53-A2C4-4D7D-83FE-D05416EBD58E"),
            [".fsproj"] = new Guid("F2A71F9B-5D33-465A-A702-920D77279786"),
            [".nativeProj"] = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"),
            [".nuproj"] = new Guid("FF286327-C783-4F7A-AB73-9BCBAD0D4460"),
            [".vcproj"] = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"),
            [".vcxproj"] = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"),
            [".vjsproj"] = new Guid("E6FDF86B-F3D1-11D4-8576-0002A516ECE8"),
            [".wixproj"] = new Guid("930C7802-8A8C-48F9-8165-68863BCCD9DD"),
        };

        public SlnProject([NotNull] string fullPath, [NotNull] string name, Guid projectGuid, Guid projectTypeGuid, [NotNull] IEnumerable<string> configurations, [NotNull] IEnumerable<string> platforms, bool isMainProject, bool isDeployable)
        {
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ProjectGuid = projectGuid;
            ProjectTypeGuid = projectTypeGuid;
            IsDeployable = isDeployable;
            IsMainProject = isMainProject;
            Configurations = configurations;
            Platforms = platforms;
        }

        public IEnumerable<string> Configurations { get; }

        public string FullPath { get; }

        public bool IsDeployable { get; }

        public bool IsMainProject { get; }

        public string Name { get; }

        public IEnumerable<string> Platforms { get; }

        public Guid ProjectGuid { get; }

        public Guid ProjectTypeGuid { get; }

        [NotNull]
        public static SlnProject FromProject([NotNull] Project project, [NotNull] IReadOnlyDictionary<string, Guid> customProjectTypeGuids, bool isMainProject = false)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (customProjectTypeGuids == null)
            {
                throw new ArgumentNullException(nameof(customProjectTypeGuids));
            }

            string name = project.GetPropertyValueOrDefault(AssemblyNamePropertyName, Path.GetFileNameWithoutExtension(project.FullPath));

            bool isUsingMicrosoftNetSdk = project.GetPropertyValue(UsingMicrosoftNetSdkPropertyName).Equals("true", StringComparison.OrdinalIgnoreCase);

            string extension = Path.GetExtension(project.FullPath);

            Guid projectTypeGuid = GetKnownProjectTypeGuid(extension, isUsingMicrosoftNetSdk, customProjectTypeGuids);

            IEnumerable<string> configurations = project.GetPossiblePropertyValuesOrDefault("Configuration", "Debug");
            IEnumerable<string> platforms = project.GetPossiblePropertyValuesOrDefault("Platform", "AnyCPU");

            Guid projectGuid = Guid.NewGuid();

            if (!isUsingMicrosoftNetSdk && !Guid.TryParse(project.GetPropertyValueOrDefault(ProjectGuidPropertyName, projectGuid.ToString()), out projectGuid))
            {
                throw new FormatException($"property ProjectGuid has an invalid format in {project.FullPath}");
            }

            string isDeployableStr = project.GetPropertyValue("SlnGenIsDeployable");

            bool isDeployable = false;

            string projectFileExtension = Path.GetExtension(project.FullPath);

            if (string.IsNullOrWhiteSpace(isDeployableStr) && !string.IsNullOrWhiteSpace(projectFileExtension) && projectFileExtension.Equals(".sfproj", StringComparison.OrdinalIgnoreCase))
            {
                isDeployable = true;
            }
            else
            {
                isDeployable = isDeployableStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            return new SlnProject(project.FullPath, name, projectGuid, projectTypeGuid, configurations, platforms, isMainProject, isDeployable);
        }

        /// <summary>
        /// Determines the project type GUID for the specified file extension.
        /// </summary>
        /// <param name="extension">The file extension of the project.</param>
        /// <param name="isUsingMicrosoftNetSdk">Indicates whether or not the project is using the Microsoft.NET.Sdk.</param>
        /// <param name="customProjectTypeGuids">A list of custom project type GUIDs to use.</param>
        /// <returns>The project type GUID for the specfied project extension.</returns>
        internal static Guid GetKnownProjectTypeGuid(string extension, bool isUsingMicrosoftNetSdk, IReadOnlyDictionary<string, Guid> customProjectTypeGuids)
        {
            if (customProjectTypeGuids.TryGetValue(extension, out Guid projectTypeGuid) || KnownProjectTypeGuids.TryGetValue(extension, out projectTypeGuid))
            {
                return projectTypeGuid;
            }

            // Use GUIDs for .NET SDK projects
            if (isUsingMicrosoftNetSdk && !KnownNetSdkProjectTypeGuids.TryGetValue(extension, out projectTypeGuid))
            {
                projectTypeGuid = DefaultNetSdkProjectTypeGuid;
            }

            // Use GUIDs for legacy projects
            if (!isUsingMicrosoftNetSdk && !KnownLegacyProjectTypeGuids.TryGetValue(extension, out projectTypeGuid))
            {
                projectTypeGuid = DefaultLegacyProjectTypeGuid;
            }

            return projectTypeGuid;
        }
    }
}