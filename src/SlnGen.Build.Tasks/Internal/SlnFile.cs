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
        private readonly List<SlnProject> _projects = new List<SlnProject>();

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly List<string> _solutionItems = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        /// <param name="fileFormatVersion">The file format version.</param>
        public SlnFile(string fileFormatVersion)
        {
            _fileFormatVersion = fileFormatVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        public SlnFile()
            : this("12.00")
        {
        }

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<string> SolutionItems => _solutionItems;

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects);
        }

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
        /// <param name="useFolders">Specifies if folders should be created.</param>
        public void Save(string path, bool useFolders)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                Save(writer, useFolders);
            }
        }

        public void Save(TextWriter writer, bool useFolders)
        {
            writer.WriteLine(Header, _fileFormatVersion);

            if (SolutionItems.Count > 0)
            {
                writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuid.ToSolutionString()}"") = "".Solution Items"", ""Solution Items"", ""{Guid.NewGuid().ToSolutionString()}"" ");
                writer.WriteLine("	ProjectSection(SolutionItems) = preProject");
                foreach (string solutionItem in SolutionItems)
                {
                    writer.WriteLine($"		{solutionItem} = {solutionItem}");
                }

                writer.WriteLine("	EndProjectSection");
                writer.WriteLine("EndProject");
            }

            foreach (SlnProject project in _projects)
            {
                writer.WriteLine($@"Project(""{project.ProjectTypeGuid.ToSolutionString()}"") = ""{project.Name}"", ""{project.FullPath}"", ""{project.ProjectGuid.ToSolutionString()}""");
                writer.WriteLine("EndProject");
            }

            SlnHierarchy hierarchy = null;

            if (useFolders && _projects.Any(i => !i.IsMainProject))
            {
                hierarchy = new SlnHierarchy(_projects);

                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    writer.WriteLine($@"Project(""{folder.ProjectTypeGuid.ToSolutionString()}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
                    writer.WriteLine("EndProject");
                }
            }

            writer.WriteLine("Global");

            if (useFolders && _projects.Count > 1)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");

                foreach (SlnFolder folder in hierarchy.Folders.Where(i => i.Parent != null))
                {
                    foreach (SlnProject project in folder.Projects)
                    {
                        writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()} = {folder.FolderGuid.ToSolutionString()}");
                    }

                    writer.WriteLine($@"		{folder.FolderGuid.ToSolutionString()} = {folder.Parent.FolderGuid.ToSolutionString()}");
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");

            HashSet<string> allPlatforms = new HashSet<string>(_projects.SelectMany(i => i.Platforms).OrderBy(i => i), StringComparer.OrdinalIgnoreCase);
            HashSet<string> allConfigurations = new HashSet<string>(_projects.SelectMany(i => i.Configurations), StringComparer.OrdinalIgnoreCase);

            foreach (string configuration in allConfigurations)
            {
                foreach (string platform in allPlatforms.Where(i => !string.Equals(i, "Win32", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                    {
                        writer.WriteLine($"		{configuration}|{platform} = {configuration}|{platform}");
                    }
                }
            }

            writer.WriteLine("	EndGlobalSection");

            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (SlnProject project in _projects)
            {
                foreach (string configuration in allConfigurations)
                {
                    foreach (string platform in allPlatforms)
                    {
                        if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                        {
                            if (project.Configurations.Contains(configuration) && project.Platforms.Contains(platform))
                            {
                                writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                                writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");

                                if (project.IsDeployable)
                                {
                                    writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Deploy.0 = {configuration}|{platform}");
                                }
                            }
                        }
                    }
                }
            }

            writer.WriteLine("	EndGlobalSection");

            writer.WriteLine("EndGlobal");
        }
    }
}
