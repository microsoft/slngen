using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// Solution class.
    /// </summary>
    internal class SolutionFile
    {
        /// <summary>
        /// The solution header
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The configurations
        /// </summary>
        private readonly string[] _configurations;

        /// <summary>
        /// The file format version
        /// </summary>
        private readonly string _fileFormatVersion;

        /// <summary>
        /// The platforms
        /// </summary>
        private readonly string[] _platforms;

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly IReadOnlyList<SolutionProject> _projects;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFile" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="configurations">The configurations.</param>
        /// <param name="platforms">The platforms.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SolutionFile(IEnumerable<SolutionProject> projects, string[] configurations, string[] platforms, string fileFormatVersion)
        {
            Guid = System.Guid.NewGuid().ToString().ToUpperInvariant();

            _projects = projects.ToList();
            _configurations = configurations;
            _platforms = platforms;
            _fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFile" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public SolutionFile(IEnumerable<SolutionProject> projects)
            : this(projects, new[] {"Debug", "Release"}, new[] {"Any CPU"}, "12.00")
        {
        }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public string Guid { get; }

        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            using (StreamWriter writer = File.CreateText(path))
            {
                writer.WriteLine(Header, _fileFormatVersion);

                foreach (SolutionProject project in _projects)
                {
                    writer.WriteLine(project.ToString());
                }

                NestedProjectsSection nestedProjects = new NestedProjectsSection(_projects);

                if (_projects.Count > 1)
                {
                    foreach (SolutionFolder folder in nestedProjects.Folders)
                    {
                        writer.WriteLine(folder.ToString());
                    }
                }

                writer.WriteLine("Global");
                writer.WriteLine(BuildSolutionConfigurationPlatforms());
                writer.WriteLine(BuildProjectConfigurationPlatforms());

                if (_projects.Count > 1)
                {
                    writer.WriteLine(nestedProjects.Build());
                }

                writer.WriteLine("EndGlobal");
            }
        }

        /// <summary>
        /// Creates a global section
        /// </summary>
        /// <returns>global section</returns>
        private static string CreateGlobalSection(string sectionName, string sectionContent)
        {
            return $@"	GlobalSection({sectionName}) = preSolution{Environment.NewLine}{sectionContent}	EndGlobalSection";
        }

        private string BuildProjectConfigurationPlatforms()
        {
            StringBuilder builder = new StringBuilder();
            foreach (SolutionProject project in _projects)
            {
                foreach (string configuration in _configurations)
                {
                    foreach (string platform in _platforms)
                    {
                        string guid = project.ProjectGuid;
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                    }
                }
            }

            return CreateGlobalSection("ProjectConfigurationPlatforms", builder.ToString());
        }

        private string BuildSolutionConfigurationPlatforms()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string configuration in _configurations)
            {
                foreach (string platform in _platforms)
                {
                    builder.AppendLine($@"		{configuration}|{platform} = {configuration}|{platform}");
                }
            }

            return CreateGlobalSection("SolutionConfigurationPlatforms", builder.ToString());
        }

        /// <summary>
        /// Nested projects section
        /// </summary>
        /// <remarks>This assumes all projects are on the same drive.</remarks>
        private class NestedProjectsSection
        {
            private readonly Dictionary<string, string> _itemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            private readonly StringBuilder _nestedProjects = new StringBuilder();

            public NestedProjectsSection(IReadOnlyList<SolutionProject> projects)
            {
                string commonPrefix = new string(
                    projects.First(e => !e.IsMainProject).FullPath.Substring(0, projects.Min(s => s.FullPath.Length))
                        .TakeWhile((c, i) => projects.All(s => s.FullPath[i] == c)).ToArray());

                Folders = new List<SolutionFolder>();

                foreach (SolutionProject project in projects)
                {
                    if (project.IsMainProject)
                    {
                        continue;
                    }

                    BuildHierarchyBottomUp(project, commonPrefix.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            public List<SolutionFolder> Folders { get; }

            public string Build()
            {
                return CreateGlobalSection("NestedProjects", _nestedProjects.ToString());
            }

            private void BuildHierarchyBottomUp(SolutionProject project, string root)
            {
                string parent = Directory.GetParent(project.FullPath).FullName;
                string currentGuid = project.ProjectGuid;

                while (true)
                {
                    bool visited = _itemId.TryGetValue(parent, out string parentGuid);
                    if (!visited)
                    {
                        parentGuid = $"{{{System.Guid.NewGuid().ToString().ToUpperInvariant()}}}";
                        _itemId.Add(parent, parentGuid);
                        Folders.Add(new SolutionFolder(parent, parentGuid));
                    }

                    _nestedProjects.AppendLine($@"		{currentGuid} = {parentGuid}");
                    if (visited || parent.Equals(root, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    currentGuid = parentGuid;
                    parent = Directory.GetParent(parent).FullName;
                }
            }
        }
    }
}