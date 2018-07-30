// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
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
        /// The file format version
        /// </summary>
        private readonly string _fileFormatVersion;

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly IReadOnlyList<SlnProject> _projects;

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly List<string> _solutionItems = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The project collection.</param>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SlnFile(IEnumerable<SlnProject> projects, string fileFormatVersion)
        {
            _projects = projects.ToList();
            _fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="projects">The projects.</param>
        public SlnFile(IEnumerable<SlnProject> projects)
            : this(projects, "12.00")
        {
        }

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<string> SolutionItems => _solutionItems;

        /// <summary>
        /// Adds the specified solution items.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(IEnumerable<string> items)
        {
            _solutionItems.AddRange(items);
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
                writer.WriteLine($@"Project(""{project.ProjectTypeGuid.ToSolutionString()}"") = ""{project.Name}"", ""{project.FullPath}"", ""{project.ProjectGuid.ToSolutionString()}""");
                writer.WriteLine("EndProject");
            }

            if (SolutionItems.Count > 0)
            {
                writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuid.ToSolutionString()}"") = ""Solution Items"", ""Solution Items"", ""{Guid.NewGuid().ToSolutionString()}"" ");
                writer.WriteLine("	ProjectSection(SolutionItems) = preProject");
                foreach (string solutionItem in SolutionItems)
                {
                    writer.WriteLine($"		{solutionItem} = {solutionItem}");
                }

                writer.WriteLine("	EndProjectSection");
                writer.WriteLine("EndProject");
            }

            SlnHierarchy hierarchy = SlnHierarchy.FromProjects(_projects);

            if (hierarchy.Folders.Count > 0)
            {
                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    writer.WriteLine($@"Project(""{folder.ProjectTypeGuid.ToSolutionString()}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
                    writer.WriteLine("EndProject");
                }
            }

            writer.WriteLine("Global");

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");

            IEnumerable<string> globalConfigurations = new HashSet<string>(_projects.SelectMany(p => p.Configurations)).Distinct();
            IEnumerable<string> globalPlatforms = new HashSet<string>(_projects.SelectMany(p => p.Platforms)).Distinct().ToList();

            foreach (string configuration in globalConfigurations)
            {
                foreach (string platform in globalPlatforms)
                {
                    if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                    {
                        writer.WriteLine($"		{configuration}|{platform} = {configuration}|{platform}");
                    }
                }
            }

            writer.WriteLine(" EndGlobalSection");

            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = preSolution");
            foreach (SlnProject project in _projects)
            {
                foreach (string configuration in project.Configurations)
                {
                    foreach (string platform in project.Platforms)
                    {
                        if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                        {
                            writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                            writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                        }
                    }
                }
            }

            writer.WriteLine(" EndGlobalSection");

            if (_projects.Count > 1)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");
                foreach (KeyValuePair<Guid, Guid> nestedProject in hierarchy.Hierarchy)
                {
                    writer.WriteLine($@"		{nestedProject.Key.ToSolutionString()} = {nestedProject.Value.ToSolutionString()}");
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("EndGlobal");
        }
    }
}