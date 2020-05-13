// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a Visual Studio solution file.
    /// </summary>
    public sealed class SlnFile
    {
        /// <summary>
        /// The beginning of the line that ends a global section.
        /// </summary>
        private const string GlobalSectionEnd = "\tEndGlobalSection";

        /// <summary>
        /// The beginning of the line that starts the extensibility global section.
        /// </summary>
        private const string GlobalSectionStartExtensibilityGlobals = "\tGlobalSection(ExtensibilityGlobals)";

        /// <summary>
        /// The solution header.
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version {0}";

        /// <summary>
        /// The beginning of the line that ends project information.
        /// </summary>
        private const string ProjectSectionEnd = "EndProject";

        /// <summary>
        /// The beginning of the line that contains project information.
        /// </summary>
        private const string ProjectSectionStart = "Project(\"";

        /// <summary>
        /// The beginning of the line that contains the solution GUID.
        /// </summary>
        private const string SectionSettingSolutionGuid = "\t\tSolutionGuid = ";

        /// <summary>
        /// The separator to split project information by.
        /// </summary>
        private static readonly string[] ProjectSectionSeparator = { "\", \"" };

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
        /// Gets or sets a <see cref="IReadOnlyCollection{String}" /> of Configuration values to use.
        /// </summary>
        public IReadOnlyCollection<string> Configurations { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing any existing project GUIDs to re-use.
        /// </summary>
        public IReadOnlyDictionary<string, Guid> ExistingProjectGuids { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="IReadOnlyCollection{String}" /> of Platform values to use.
        /// </summary>
        public IReadOnlyCollection<string> Platforms { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="Guid" /> for the solution file.
        /// </summary>
        public Guid SolutionGuid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets a list of solution items.
        /// </summary>
        public IReadOnlyCollection<string> SolutionItems => _solutionItems;

        /// <summary>
        /// Attempts to read the existing GUID from a solution file if one exists.
        /// </summary>
        /// <param name="path">The path to a solution file.</param>
        /// <param name="solutionGuid">Receives the <see cref="Guid" /> of the existing solution file if one is found, otherwise default(Guid).</param>
        /// <param name="projectGuidsByPath">Receives the project GUIDs by their full paths.</param>
        /// <returns>true if the solution GUID was found, otherwise false.</returns>
        public static bool TryParseExistingSolution(string path, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath)
        {
            solutionGuid = default;
            projectGuidsByPath = default;

            bool foundSolutionGuid = false;

            Dictionary<string, Guid> projectGuids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            FileInfo fileInfo = new FileInfo(path);

            if (!fileInfo.Exists || fileInfo.Directory == null)
            {
                return false;
            }

            string solutionFileDirectory = fileInfo.Directory.FullName;

            using (FileStream stream = File.OpenRead(path))
            using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(0), detectEncodingFromByteOrderMarks: true))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(ProjectSectionStart))
                    {
                        string[] projectDetails = line.Split(ProjectSectionSeparator, StringSplitOptions.RemoveEmptyEntries);

                        if (projectDetails.Length == 3)
                        {
                            FileInfo projectFullPath = new FileInfo(Path.Combine(solutionFileDirectory, projectDetails[1].Trim().Trim('\"')));

                            if (projectFullPath.Exists && Guid.TryParse(projectDetails[2].Trim('\"'), out Guid projectGuid))
                            {
                                projectGuids[projectFullPath.FullName] = projectGuid;
                            }
                        }

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith(ProjectSectionEnd))
                            {
                                break;
                            }
                        }
                    }

                    if (line != null && line.StartsWith(GlobalSectionStartExtensibilityGlobals))
                    {
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.StartsWith(SectionSettingSolutionGuid))
                            {
                                string solutionGuidString = line.Substring(SectionSettingSolutionGuid.Length);

                                foundSolutionGuid = Guid.TryParse(solutionGuidString, out solutionGuid);
                            }

                            if (line.StartsWith(GlobalSectionEnd))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            projectGuidsByPath = projectGuids;

            return foundSolutionGuid;
        }

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects.Select(project =>
            {
                if (project != null && ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(project.FullPath, out Guid projectGuid))
                {
                    project.ProjectGuid = projectGuid;
                }

                return project;
            }));
        }

        /// <summary>
        /// Adds the specified projects to the solution file.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{T}" /> of projects to add.</param>
        /// <param name="customProjectTypeGuids">An <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing any custom project type GUIDs to use.</param>
        /// <param name="mainProjectFullPath">The full path to the main project.</param>
        public void AddProjects(IEnumerable<Project> projects, IReadOnlyDictionary<string, Guid> customProjectTypeGuids, string mainProjectFullPath)
        {
            _projects.AddRange(
                projects
                    .Distinct(new EqualityComparer<Project>((x, y) => string.Equals(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase), i => i.FullPath.GetHashCode()))
                    .Select(i =>
                    {
                        SlnProject project = SlnProject.FromProject(i, customProjectTypeGuids, string.Equals(i.FullPath, mainProjectFullPath, StringComparison.OrdinalIgnoreCase));

                        if (project != null && ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(project.FullPath, out Guid projectGuid))
                        {
                            project.ProjectGuid = projectGuid;
                        }

                        return project;
                    })
                    .Where(i => i != null)
                    .OrderBy(i => i.FullPath));
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
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        public void Save(string path, bool useFolders, bool collapseFolders = false)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!directoryName.IsNullOrWhiteSpace())
            {
                Directory.CreateDirectory(directoryName);
            }

            using (FileStream fileStream = File.Create(path))
            using (StreamWriter writer = new StreamWriter(fileStream, Encoding.Unicode))
            {
                Save(writer, useFolders, collapseFolders);
            }
        }

        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to save the solution file to.</param>
        /// <param name="useFolders">Specifies if folders should be created.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        internal void Save(TextWriter writer, bool useFolders, bool collapseFolders = false)
        {
            writer.WriteLine(Header, _fileFormatVersion);

            if (SolutionItems.Count > 0)
            {
                writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuid}"") = ""Solution Items"", ""Solution Items"", ""{Guid.NewGuid().ToSolutionString()}"" ");
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
                hierarchy = new SlnHierarchy(_projects, collapseFolders);

                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    writer.WriteLine($@"Project(""{folder.ProjectTypeGuid}"") = ""{folder.Name}"", ""{folder.FullPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
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

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");

            HashSet<string> solutionPlatforms = Platforms != null && Platforms.Any()
                ? new HashSet<string>(GetValidSolutionPlatforms(Platforms), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(GetValidSolutionPlatforms(_projects.SelectMany(i => i.Platforms)), StringComparer.OrdinalIgnoreCase);

            HashSet<string> solutionConfigurations = Configurations != null && Configurations.Any()
                ? new HashSet<string>(Configurations, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(_projects.SelectMany(i => i.Configurations).Where(i => !i.IsNullOrWhiteSpace()), StringComparer.OrdinalIgnoreCase);

            foreach (string configuration in solutionConfigurations)
            {
                foreach (string platform in solutionPlatforms)
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
                string projectGuid = project.ProjectGuid.ToSolutionString();

                foreach (string configuration in solutionConfigurations)
                {
                    bool foundConfiguration = TryGetProjectSolutionConfiguration(configuration, project, out string projectSolutionConfiguration);

                    foreach (string platform in solutionPlatforms)
                    {
                        bool foundPlatform = TryGetProjectSolutionPlatform(platform, project, out string projectSolutionPlatform, out string projectBuildPlatform);

                        writer.WriteLine($@"		{projectGuid}.{configuration}|{platform}.ActiveCfg = {projectSolutionConfiguration}|{projectSolutionPlatform}");

                        if (foundPlatform && foundConfiguration)
                        {
                            writer.WriteLine($@"		{projectGuid}.{configuration}|{platform}.Build.0 = {projectSolutionConfiguration}|{projectBuildPlatform}");
                        }

                        if (project.IsDeployable)
                        {
                            writer.WriteLine($@"		{projectGuid}.{configuration}|{platform}.Deploy.0 = {projectSolutionConfiguration}|{projectSolutionPlatform}");
                        }
                    }
                }
            }

            writer.WriteLine("	EndGlobalSection");

            writer.WriteLine("	GlobalSection(SolutionProperties) = preSolution");
            writer.WriteLine("		HideSolutionNode = FALSE");
            writer.WriteLine("	EndGlobalSection");

            writer.WriteLine("	GlobalSection(ExtensibilityGlobals) = postSolution");
            writer.WriteLine($"		SolutionGuid = {SolutionGuid.ToSolutionString()}");
            writer.WriteLine("	EndGlobalSection");

            writer.WriteLine("EndGlobal");
        }

        private IEnumerable<string> GetValidSolutionPlatforms(IEnumerable<string> platforms)
        {
            List<string> values = platforms
                .Select(i => i.ToSolutionPlatform())
                .Where(platform =>
                {
                    switch (platform.ToLowerInvariant())
                    {
                        case "any cpu":
                        case "x64":
                        case "x86":
                            return true;

                        default:
                            return false;
                    }
                }).OrderBy(i => i)
                .ToList();

            return values.Any() ? values : new List<string> { "Any CPU" };
        }

        private bool TryGetProjectSolutionConfiguration(string solutionConfiguration, SlnProject project, out string projectSolutionConfiguration)
        {
            foreach (string projectConfiguration in project.Configurations)
            {
                if (string.Equals(projectConfiguration, solutionConfiguration, StringComparison.OrdinalIgnoreCase))
                {
                    projectSolutionConfiguration = solutionConfiguration;

                    return true;
                }
            }

            projectSolutionConfiguration = project.Configurations.First();

            return false;
        }

        private bool TryGetProjectSolutionPlatform(string solutionPlatform, SlnProject project, out string projectSolutionPlatform, out string projectBuildPlatform)
        {
            projectSolutionPlatform = null;
            projectBuildPlatform = null;

            bool containsWin32 = false;
            bool containsX64 = false;
            bool containsAmd64 = false;
            bool containsX86 = false;
            bool containsAnyCPU = false;

            foreach (string projectPlatform in project.Platforms)
            {
                if (string.Equals(projectPlatform, solutionPlatform, StringComparison.OrdinalIgnoreCase) || string.Equals(projectPlatform.ToSolutionPlatform(), solutionPlatform, StringComparison.OrdinalIgnoreCase))
                {
                    projectSolutionPlatform = solutionPlatform;

                    projectBuildPlatform = solutionPlatform;

                    return true;
                }

                switch (projectPlatform.ToLowerInvariant())
                {
                    case "anycpu":
                    case "any cpu":
                        containsAnyCPU = true;
                        break;

                    case "x64":
                        containsX64 = true;
                        break;

                    case "x86":
                        containsX86 = true;
                        break;

                    case "amd64":
                        containsAmd64 = true;
                        break;

                    case "win32":
                        containsWin32 = true;
                        break;
                }
            }

            if (string.Equals(solutionPlatform, "Any CPU", StringComparison.OrdinalIgnoreCase))
            {
                if (containsX64)
                {
                    projectSolutionPlatform = projectBuildPlatform = "x64";

                    return true;
                }

                if (containsX86)
                {
                    projectSolutionPlatform = projectBuildPlatform = "x86";

                    return true;
                }

                if (containsAmd64)
                {
                    projectSolutionPlatform = projectBuildPlatform = "amd64";

                    return true;
                }

                if (containsWin32)
                {
                    projectSolutionPlatform = projectBuildPlatform = "Win32";

                    return true;
                }
            }

            if (string.Equals(solutionPlatform, "x86", StringComparison.OrdinalIgnoreCase))
            {
                if (containsWin32)
                {
                    projectSolutionPlatform = projectBuildPlatform = "Win32";

                    return true;
                }

                if (containsAnyCPU)
                {
                    projectSolutionPlatform = projectBuildPlatform = "Any CPU";

                    return true;
                }
            }

            if (string.Equals(solutionPlatform, "x64", StringComparison.OrdinalIgnoreCase))
            {
                if (containsAmd64)
                {
                    projectSolutionPlatform = projectBuildPlatform = "amd64";

                    return true;
                }

                if (containsAnyCPU)
                {
                    projectSolutionPlatform = projectBuildPlatform = "Any CPU";

                    return true;
                }
            }

            projectSolutionPlatform = project.Platforms.First().ToSolutionPlatform();

            return false;
        }
    }
}