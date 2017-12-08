using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace SlnGen.Build.Tasks
{
    internal class SolutionProject
    {
        public const string UsingMicrosoftNetSdkPropertyName = "UsingMicrosoftNETSdk";

        private static readonly IReadOnlyDictionary<string, string> KnownProjectTypeGuids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".ccproj", "151D2E53-A2C4-4D7D-83FE-D05416EBD58E"},
            {".csproj", "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"},
            {".nativeProj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".nuproj", "FF286327-C783-4F7A-AB73-9BCBAD0D4460"},
            {".vbproj", "F184B08F-C81C-45F6-A57F-5ABD9991F28F"},
            {".vcproj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".vcxproj", "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"},
            {".vjsproj", "E6FDF86B-F3D1-11D4-8576-0002A516ECE8"},
            {".wixproj", "930C7802-8A8C-48F9-8165-68863BCCD9DD"}
        };

        public SolutionProject(Project project, bool isMainProject)
        {
            // Legacy projects do not set UsingMicrosoftNETSdk to "true"
            bool isLegacyProjectSystem = !project.GetPropertyValue(UsingMicrosoftNetSdkPropertyName).Equals("true", StringComparison.OrdinalIgnoreCase);

            FullPath = project.FullPath;

            ProjectTypeGuid = GetProjectTypeGuid(project);

            ProjectName = project.GetPropertyValue("AssemblyName", Path.GetFileNameWithoutExtension(project.FullPath));

            ProjectGuid = isLegacyProjectSystem ? project.GetPropertyValue("ProjectGuid", Guid.NewGuid().ToString().ToUpperInvariant()) : Guid.NewGuid().ToString().ToUpperInvariant();

            IsMainProject = isMainProject;
        }

        public string FullPath { get; }

        public bool IsMainProject { get; }

        public string ProjectGuid { get; }

        public string ProjectName { get; }

        public string ProjectTypeGuid { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $@"Project(""{ProjectTypeGuid}"") = ""{ProjectName}"", ""{FullPath}"", ""{ProjectGuid}""{Environment.NewLine}EndProject";
        }

        private static string GetProjectTypeGuid(Project project)
        {
            string extension = Path.GetExtension(project.FullPath);

            if (String.IsNullOrWhiteSpace(extension) || !KnownProjectTypeGuids.TryGetValue(extension, out string type))
            {
                type = KnownProjectTypeGuids[".csproj"];
            }

            return type;
        }
    }
}