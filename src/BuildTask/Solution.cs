namespace SlnGen.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.Build.Evaluation;

    /// <summary>
    /// Solution class.
    /// </summary>
    internal class Solution
    {
        /// <summary>
        /// The solution header
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The file format version
        /// </summary>
        private readonly string fileFormatVersion;

        /// <summary>
        /// The configurations
        /// </summary>
        private readonly string[] configurations;

        /// <summary>
        /// The platforms
        /// </summary>
        private readonly string[] platforms;

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly IEnumerable<ProjectInfo> projects;

        /// <summary>
        /// Initializes a new instance of the <see cref="Solution" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="configurations">The configurations.</param>
        /// <param name="platforms">The platforms.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public Solution(IEnumerable<ProjectInfo> projects, string[] configurations, string[] platforms, string fileFormatVersion)
        {
            this.Guid = System.Guid.NewGuid().ToString().ToUpperInvariant();

            this.projects = projects;
            this.configurations = configurations;
            this.platforms = platforms;
            this.fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Solution" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public Solution(IEnumerable<ProjectInfo> projects)
            : this(projects, new[] { "Debug", "Release" }, new[] { "Any CPU" }, "12.00")
        {
        }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public string Guid { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder(string.Format(Header, this.fileFormatVersion));
            builder.AppendLine();
            foreach (var project in this.projects)
            {
                builder.AppendLine(project.ToString());
            }

            var nestedProjects = new NestedProjectsSection(this.projects);

            if (this.projects.Count() > 1)
            {
                foreach (var folder in nestedProjects.Folders)
                {
                    builder.AppendLine(folder.ToString());
                }
            }

            builder.AppendLine("Global");
            builder.AppendLine(this.BuildSolutionConfigurationPlatforms());
            builder.AppendLine(this.BuildProjectConfigurationPlatforms());

            if (this.projects.Count() > 1)
            {
                builder.AppendLine(nestedProjects.Build());
            }

            builder.AppendLine("EndGlobal");

            return builder.ToString();
        }

        /// <summary>
        /// Creates a global section
        /// </summary>
        /// <returns>global section</returns>
        private static string CreateGlobalSection(string sectionName, string sectionContent)
        {
            return $@"	GlobalSection({sectionName}) = preSolution{Environment.NewLine}{sectionContent}	EndGlobalSection";
        }

        private string BuildSolutionConfigurationPlatforms()
        {
            var builder = new StringBuilder();
            foreach (var configuration in this.configurations)
            {
                foreach (var platform in this.platforms)
                {
                    builder.AppendLine($@"		{configuration}|{platform} = {configuration}|{platform}");
                }
            }

            return CreateGlobalSection("SolutionConfigurationPlatforms", builder.ToString());
        }

        private string BuildProjectConfigurationPlatforms()
        {
            var builder = new StringBuilder();
            foreach (var project in this.projects)
            {
                foreach (var configuration in this.configurations)
                {
                    foreach (var platform in this.platforms)
                    {
                        var guid = project.Guid;
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                    }
                }
            }

            return CreateGlobalSection("ProjectConfigurationPlatforms", builder.ToString());
        }

        /// <summary>
        /// Nested projects section
        /// </summary>
        /// <remarks>This assumes all projects are on the same drive.</remarks>
        private class NestedProjectsSection
        {
            private readonly Dictionary<string, string> itemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private StringBuilder nestedProjects = new StringBuilder();

            public NestedProjectsSection(IEnumerable<ProjectInfo> projects)
            {
                var commonPrefix = new string(
                    projects.First(e => !e.Default).FullPath.Substring(0, projects.Min(s => s.FullPath.Length))
                        .TakeWhile((c, i) => projects.All(s => s.FullPath[i] == c)).ToArray());
                this.Folders = new List<FolderInfo>();
                foreach (var project in projects)
                {
                    if (project.Default)
                    {
                        continue;
                    }

                    this.BuildHierarchyBottomUp(project, commonPrefix.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            public List<FolderInfo> Folders { get; }

            private void BuildHierarchyBottomUp(ProjectInfo project, string root)
            {
                var parent = Directory.GetParent(project.FullPath).FullName;
                var currentGuid = project.Guid;

                while (true)
                {
                    var visited = this.itemId.TryGetValue(parent, out string parentGuid);
                    if (!visited)
                    {
                        parentGuid = $"{{{System.Guid.NewGuid().ToString().ToUpperInvariant()}}}";
                        this.itemId.Add(parent, parentGuid);
                        this.Folders.Add(new FolderInfo(parent, parentGuid));
                    }

                    this.nestedProjects.AppendLine($@"		{currentGuid} = {parentGuid}");
                    if (visited || parent.Equals(root, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    
                    currentGuid = parentGuid;
                    parent = Directory.GetParent(parent).FullName;
                }
            }

            public string Build()
            {
                return CreateGlobalSection("NestedProjects", this.nestedProjects.ToString());
            }
        }
    }
}
