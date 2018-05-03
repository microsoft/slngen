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
        public const string DefaultProjectTypeGuid = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

        public static readonly IReadOnlyDictionary<string, string> KnownProjectTypeGuids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".ccproj", "151D2E53-A2C4-4D7D-83FE-D05416EBD58E"},
            {".csproj", DefaultProjectTypeGuid},
            {".fsproj", "F2A71F9B-5D33-465A-A702-920D77279786" },
            {".nativeProj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".nuproj", "FF286327-C783-4F7A-AB73-9BCBAD0D4460"},
            {".vbproj", "F184B08F-C81C-45F6-A57F-5ABD9991F28F"},
            {".vcproj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".vcxproj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".vjsproj", "E6FDF86B-F3D1-11D4-8576-0002A516ECE8"},
            {".wixproj", "930C7802-8A8C-48F9-8165-68863BCCD9DD"},
        };

        public SlnProject([NotNull] string fullPath, [NotNull] string name, [NotNull] string projectGuid, [NotNull] string projectTypeGuid, [NotNull] IEnumerable<string> configurations, [NotNull] IEnumerable<string> platforms, bool isMainProject)
        {
            FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ProjectGuid = projectGuid ?? throw new ArgumentNullException(nameof(projectGuid));
            ProjectTypeGuid = projectTypeGuid ?? throw new ArgumentNullException(nameof(projectTypeGuid));
            IsMainProject = isMainProject;
            Configurations = configurations;
            Platforms = platforms;
        }

        public string FullPath { get; }

        public bool IsMainProject { get; }

        public string Name { get; }

        public string ProjectGuid { get; }

        public string ProjectTypeGuid { get; }

        public IEnumerable<string> Configurations { get; }

        public IEnumerable<string> Platforms { get; }

        [NotNull]
        public static SlnProject FromProject([NotNull] Project project, [NotNull] IReadOnlyDictionary<string, string> customProjectTypeGuids, bool isMainProject = false)
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

            // Legacy projects do not set UsingMicrosoftNETSdk to "true"
            bool isLegacyProjectSystem = !project.GetPropertyValue(UsingMicrosoftNetSdkPropertyName).Equals("true", StringComparison.OrdinalIgnoreCase);

            string extension = Path.GetExtension(project.FullPath);

            // Get the item from the custom project type GUIDs first which can override ours
            if (String.IsNullOrWhiteSpace(extension) || !customProjectTypeGuids.TryGetValue(extension, out string projectTypeGuid) && !KnownProjectTypeGuids.TryGetValue(extension, out projectTypeGuid))
            {
                projectTypeGuid = DefaultProjectTypeGuid;
            }

            IEnumerable<string> configurations = project.GetPossiblePropertyValuesOrDefault("Configuration", "Debug");
            IEnumerable<string> platforms = project.GetPossiblePropertyValuesOrDefault("Platform", "AnyCPU");

            string projectGuid = isLegacyProjectSystem ? project.GetPropertyValueOrDefault(ProjectGuidPropertyName, Guid.NewGuid().ToSolutionString()) : Guid.NewGuid().ToSolutionString();
            projectGuid = $"{{{projectGuid.ToUpper().Replace(" ","").Trim('{','}')}}}";

            return new SlnProject(project.FullPath, name, projectGuid, projectTypeGuid, configurations, platforms, isMainProject);
        }
    }
}