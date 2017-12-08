namespace SlnGen.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Build.Evaluation;

    internal class ProjectInfo
    {
        private static readonly Dictionary<string, string> ProjectTypeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".csproj",        "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC" },
            { ".ccproj",        "151D2E53-A2C4-4D7D-83FE-D05416EBD58E" },
            { ".vjsproj",       "E6FDF86B-F3D1-11D4-8576-0002A516ECE8" },
            { ".vbproj",        "F184B08F-C81C-45F6-A57F-5ABD9991F28F" },
            { ".vcproj",        "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942" },
            { ".vcxproj",       "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942" },
            { ".nativeProj",    "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942" },
            { ".nuproj",        "FF286327-C783-4F7A-AB73-9BCBAD0D4460" },
            { ".wixproj",       "930C7802-8A8C-48F9-8165-68863BCCD9DD" }
        };


        public ProjectInfo(Project project, bool @default)
        {
            var legacy = !bool.Parse(project.GetPropertyValueOrDefault("UsingMicrosoftNETSdk", "false"));
            this.FullPath = project.FullPath;
            this.TypeGuid = ExtractTypeGuid(project);
            this.AssemblyName = project.GetPropertyValue("AssemblyName");
            var guid = $"{{{System.Guid.NewGuid().ToString().ToUpperInvariant()}}}";
            this.Guid = legacy ? project.GetPropertyValueOrDefault("ProjectGuid") ?? guid : guid;
            this.Default = @default;
        }

        public bool Default { get; }

        public string Guid { get; }

        public string TypeGuid { get; }

        public string AssemblyName { get; }

        public string FullPath { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $@"Project(""{this.TypeGuid}"") = ""{this.AssemblyName}"", ""{this.FullPath}"", ""{this.Guid}""{Environment.NewLine}EndProject";
        }

        private static string ExtractTypeGuid(Project project)
        {
            // default is CSharp
            var type = ProjectTypeMapping[".csproj"];
            var extension = Path.GetExtension(project.FullPath);
            if (ProjectTypeMapping.ContainsKey(extension))
            {
                type = ProjectTypeMapping[extension];
            }

            return type;
        }
    }
}
