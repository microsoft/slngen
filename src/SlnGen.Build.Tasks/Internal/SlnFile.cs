using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnGen.Build.Tasks.Internal
{
    internal sealed class SlnFile
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
        private readonly IReadOnlyList<SlnProject> _projects;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="configurations">The configurations.</param>
        /// <param name="platforms">The platforms.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SlnFile(IEnumerable<SlnProject> projects, string[] configurations, string[] platforms, string fileFormatVersion)
        {
            _projects = projects.ToList();
            _configurations = configurations;
            _platforms = platforms;
            _fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public SlnFile(IEnumerable<SlnProject> projects)
            : this(projects, new[] {"Debug", "Release"}, new[] {"Any CPU"}, "12.00")
        {
        }

        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="path">The full path to the file to write to.</param>
        public void Save(string path)
        {
            using (StreamWriter writer = File.CreateText(path))
            {
                Save(writer);
            }
        }

        public void Save(TextWriter writer)
        {
            writer.WriteLine(Header, _fileFormatVersion);

            foreach (SlnProject project in _projects)
            {
                writer.WriteLine($@"Project(""{project.ProjectTypeGuid}"") = ""{project.Name}"", ""{project.FullPath}"", ""{project.ProjectGuid}""");
                writer.WriteLine("EndProject");
            }

            SlnHierarchy hierarchy = SlnHierarchy.FromProjects(_projects);

            if (hierarchy.Folders.Count > 0)
            {
                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    writer.WriteLine($@"Project(""{folder.TypeGuid}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.Guid}""");
                    writer.WriteLine("EndProject");
                }
            }

            writer.WriteLine("Global");

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach (string configuration in _configurations)
            {
                foreach (string platform in _platforms)
                {
                    writer.WriteLine($"		{configuration}|{platform} = {configuration}|{platform}");
                }
            }
            writer.WriteLine(" EndGlobalSection");

            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = preSolution");
            foreach (SlnProject project in _projects)
            {
                foreach (string configuration in _configurations)
                {
                    foreach (string platform in _platforms)
                    {
                        writer.WriteLine($@"		{project.ProjectGuid}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                        writer.WriteLine($@"		{project.ProjectGuid}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                    }
                }
            }
            writer.WriteLine(" EndGlobalSection");

            if (_projects.Count > 1)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");
                foreach (KeyValuePair<string, string> nestedProject in hierarchy.Hierarchy)
                {
                    writer.WriteLine($@"		{nestedProject.Key} = {nestedProject.Value}");
                }
                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("EndGlobal");
        }
    }
}