﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnGen.Common
{
    public sealed class SlnFile
    {
        /// <summary>
        /// The solution header.
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The file format version.
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
        /// A <see cref="IReadOnlyCollection{String}" /> of Configuration values to use.
        /// </summary>
        public IReadOnlyCollection<string> Configurations { get; set; }

        /// <summary>
        /// A <see cref="IReadOnlyCollection{String}" /> of Platform values to use.
        /// </summary>
        public IReadOnlyCollection<string> Platforms { get; set; }

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<string> SolutionItems => _solutionItems;

        /// <summary>
        /// Gets the solution items' full paths.
        /// </summary>
        /// <param name="items">The <see cref="IEnumerable{IMSBuildItem}" /> containing the solution items.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <returns>An <see cref="IEnumerable{String}"/> of full paths to include as solution items.</returns>
        public static IEnumerable<string> GetSolutionItems(IEnumerable<IMSBuildItem> items, ISlnGenLogger logger)
        {
            return GetSolutionItems(items, logger, File.Exists);
        }

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects);
        }

        public void AddProjects(ProjectCollection projectCollection, IReadOnlyDictionary<string, Guid> customProjectTypeGuids, string mainProjectFullPath)
        {
            _projects.AddRange(
                projectCollection
                    .LoadedProjects
                    .Distinct(new EqualityComparer<Project>((x, y) => string.Equals(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase), i => i.FullPath.GetHashCode()))
                    .Select(i => SlnProject.FromProject(i, customProjectTypeGuids, string.Equals(i.FullPath, mainProjectFullPath, StringComparison.OrdinalIgnoreCase)))
                    .Where(i => i != null));
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
        /// <param name="enableConfigurationAndPlatforms">Specifies if configuration and platform values should be generated in the solution.</param>
        public void Save(string path, bool useFolders, bool enableConfigurationAndPlatforms = true)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                Save(writer, useFolders, enableConfigurationAndPlatforms);
            }
        }

        public void Save(TextWriter writer, bool useFolders, bool enableConfigurationAndPlatforms = true)
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

            foreach (SlnProject project in _projects.OrderBy(i => i.FullPath))
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

            if (useFolders && _projects.Count > 1 && hierarchy != null)
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

            if (enableConfigurationAndPlatforms)
            {
                writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");

                HashSet<string> allPlatforms = Platforms != null && Platforms.Any()
                    ? new HashSet<string>(Platforms)
                    : new HashSet<string>(_projects.SelectMany(i => i.Platforms).Where(i => !string.Equals(i, "Win32", StringComparison.OrdinalIgnoreCase)).OrderBy(i => i), StringComparer.OrdinalIgnoreCase);

                HashSet<string> allConfigurations = Configurations != null && Configurations.Any()
                    ? new HashSet<string>(Configurations)
                    : new HashSet<string>(_projects.SelectMany(i => i.Configurations), StringComparer.OrdinalIgnoreCase);

                foreach (string configuration in allConfigurations)
                {
                    foreach (string platform in allPlatforms)
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
                                    string activeCfgPlatform = project.Platform.IsNullOrWhitespace() && string.Equals("x86", platform) && project.Platforms.Contains("Win32") ? "Win32" : platform;

                                    writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.ActiveCfg = {configuration}|{activeCfgPlatform}");
                                    writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");

                                    if (project.IsDeployable)
                                    {
                                        writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Deploy.0 = {configuration}|{platform}");
                                    }
                                }
                                else
                                {
                                    string actualPlatform = project.Platforms.Count == 1 ? project.Platforms.First() : platform;

                                    writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.ActiveCfg = {configuration}|{actualPlatform}");
                                    writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()}.{configuration}|{platform}.Build.0 = {configuration}|{actualPlatform}");
                                }
                            }
                        }
                    }
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("EndGlobal");
        }

        /// <summary>
        /// Gets the solution items' full paths.
        /// </summary>
        /// <param name="items">The <see cref="IEnumerable{IMSBuildItem}" /> containing the solution items.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <param name="fileExists">A <see cref="Func{String, Boolean}"/> to use when determining if a file exists.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items" /> is <code>null</code>
        /// -or-
        /// <paramref name="logger" /> is <code>null</code>
        /// -or-
        /// <paramref name="fileExists" /> is <code>null</code>.</exception>
        /// <returns>An <see cref="IEnumerable{String}"/> of full paths to include as solution items.</returns>
        internal static IEnumerable<string> GetSolutionItems(IEnumerable<IMSBuildItem> items, ISlnGenLogger logger, Func<string, bool> fileExists)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (fileExists == null)
            {
                throw new ArgumentNullException(nameof(fileExists));
            }

            foreach (string solutionItem in items.Select(i => i.GetMetadata("FullPath")).Where(i => !string.IsNullOrWhiteSpace(i)))
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
    }
}