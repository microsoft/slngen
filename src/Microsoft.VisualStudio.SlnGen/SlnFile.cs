// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
        /// A regular expression used to parse the project section.
        /// </summary>
        private static readonly Regex GuidRegex = new (@"(?<Guid>\{[0-9a-fA-F\-]+\})");

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
        private readonly List<SlnProject> _projects = new ();

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly List<string> _solutionItems = new ();

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
        /// Gets or sets an optional minumal Visual Studio version for the solution file.
        /// </summary>
        public string MinumumVisualStudioVersion { get; set; } = "10.0.40219.1";

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
        /// Gets or sets an optional Visual Studio version for the solution file.
        /// </summary>
        public Version VisualStudioVersion { get; set; }

        /// <summary>
        /// Generates a solution file.
        /// </summary>
        /// <param name="arguments">The current <see cref="ProgramArguments" />.</param>
        /// <param name="projects">A <see cref="IEnumerable{String}" /> containing the entry projects.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <returns>A <see cref="Tuple{String, Int32, Int32, Guid}" /> with the full path to the solution file, the count of custom project type GUIDs used, the count of solution items, and the solution GUID.</returns>
        public static (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid) GenerateSolutionFile(ProgramArguments arguments, IEnumerable<Project> projects, ISlnGenLogger logger)
        {
            List<Project> projectList = projects.ToList();

            Project firstProject = projectList.First();

            IReadOnlyDictionary<string, Guid> customProjectTypeGuids = SlnProject.GetCustomProjectTypeGuids(firstProject);

            IReadOnlyCollection<string> solutionItems = SlnProject.GetSolutionItems(firstProject, logger).ToList();

            string solutionFileFullPath = arguments.SolutionFileFullPath?.LastOrDefault();

            if (solutionFileFullPath.IsNullOrWhiteSpace())
            {
                string solutionDirectoryFullPath = arguments.SolutionDirectoryFullPath?.LastOrDefault();

                if (solutionDirectoryFullPath.IsNullOrWhiteSpace())
                {
                    solutionDirectoryFullPath = firstProject.DirectoryPath;
                }

                string solutionFileName = Path.ChangeExtension(Path.GetFileName(firstProject.FullPath), "sln");

                solutionFileFullPath = Path.Combine(solutionDirectoryFullPath!, solutionFileName);
            }

            logger.LogMessageHigh($"Generating Visual Studio solution \"{Path.GetFullPath(solutionFileFullPath)}\" ...");

            if (customProjectTypeGuids.Count > 0)
            {
                logger.LogMessageLow("Custom Project Type GUIDs:");
                foreach (KeyValuePair<string, Guid> item in customProjectTypeGuids)
                {
                    logger.LogMessageLow("  {0} = {1}", item.Key, item.Value);
                }
            }

            SlnFile solution = new SlnFile
            {
                Platforms = arguments.GetPlatforms(),
                Configurations = arguments.GetConfigurations(),
            };

            if (arguments.VisualStudioVersion.HasValue)
            {
                if (arguments.VisualStudioVersion.Version != null && Version.TryParse(arguments.VisualStudioVersion.Version, out Version version))
                {
                    solution.VisualStudioVersion = version;
                }

                if (solution.VisualStudioVersion == null)
                {
                    string devEnvFullPath = arguments.GetDevEnvFullPath(Program.CurrentDevelopmentEnvironment.VisualStudio);

                    if (!devEnvFullPath.IsNullOrWhiteSpace() && File.Exists(devEnvFullPath))
                    {
                        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(devEnvFullPath);

                        solution.VisualStudioVersion = new Version(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart, fileVersionInfo.FilePrivatePart);
                    }
                }
            }

            if (SlnFile.TryParseExistingSolution(solutionFileFullPath, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath))
            {
                logger.LogMessageNormal("Updating existing solution file and reusing Visual Studio cache");

                solution.SolutionGuid = solutionGuid;
                solution.ExistingProjectGuids = projectGuidsByPath;

                arguments.LoadProjectsInVisualStudio = new[] { bool.TrueString };
            }

            solution.AddProjects(projectList, customProjectTypeGuids, arguments.IgnoreMainProject ? null : firstProject.FullPath);

            solution.AddSolutionItems(solutionItems);

            if (!arguments.EnableFolders())
            {
                foreach (SlnProject project in solution._projects.Distinct(new EqualityComparer<SlnProject>((x, y) => string.Equals(x.SolutionFolder, y.SolutionFolder)))
                    .Where(i => !string.IsNullOrWhiteSpace(i.SolutionFolder) && i.SolutionFolder.Contains(Path.DirectorySeparatorChar)))
                {
                    logger.LogError($"The value of SlnGenSolutionFolder \"{project.SolutionFolder}\" cannot contain directory separators.");
                }
            }

            if (!logger.HasLoggedErrors)
            {
                solution.Save(solutionFileFullPath, arguments.EnableFolders(), arguments.EnableCollapseFolders(), arguments.EnableAlwaysBuild());
            }

            return (solutionFileFullPath, customProjectTypeGuids.Count, solutionItems.Count, solution.SolutionGuid);
        }

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

            using FileStream stream = File.OpenRead(path);
            using StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(0), detectEncodingFromByteOrderMarks: true);

            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(ProjectSectionStart))
                {
                    string[] projectDetails = line.Split(ProjectSectionSeparator, StringSplitOptions.RemoveEmptyEntries);

                    if (projectDetails.Length == 3)
                    {
                        Match projectGuidMatch = GuidRegex.Match(projectDetails[2]);

                        if (!projectGuidMatch.Groups["Guid"].Success)
                        {
                            continue;
                        }

                        string projectGuidString = projectGuidMatch.Groups["Guid"].Value;

                        Match projectTypeGuidMatch = GuidRegex.Match(projectDetails[0]);

                        if (!projectTypeGuidMatch.Groups["Guid"].Success)
                        {
                            continue;
                        }

                        if (!Guid.TryParse(projectGuidString, out Guid projectGuid) || !Guid.TryParse(projectTypeGuidMatch.Groups["Guid"].Value, out Guid projectTypeGuid))
                        {
                            continue;
                        }

                        string projectPath = projectDetails[1].Trim().Trim('\"');

                        projectGuids[projectPath] = projectGuid;
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

            projectGuidsByPath = projectGuids;

            return foundSolutionGuid;
        }

        /// <summary>
        /// Adds the specified projects.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{SlnProject}"/> containing projects to add to the solution.</param>
        public void AddProjects(IEnumerable<SlnProject> projects)
        {
            _projects.AddRange(projects);
        }

        /// <summary>
        /// Adds the specified projects to the solution file.
        /// </summary>
        /// <param name="projects">An <see cref="IEnumerable{T}" /> of projects to add.</param>
        /// <param name="customProjectTypeGuids">An <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing any custom project type GUIDs to use.</param>
        /// <param name="mainProjectFullPath">Optional full path to the main project.</param>
        public void AddProjects(IEnumerable<Project> projects, IReadOnlyDictionary<string, Guid> customProjectTypeGuids, string mainProjectFullPath = null)
        {
            _projects.AddRange(
                projects
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
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <param name="alwaysBuild">An optional value indicating whether or not to always include the project in the build even if it has no matching configuration.</param>
        public void Save(string path, bool useFolders, bool collapseFolders = false, bool alwaysBuild = true)
        {
            string directoryName = Path.GetDirectoryName(path);

            if (!directoryName.IsNullOrWhiteSpace())
            {
                Directory.CreateDirectory(directoryName!);
            }

            using FileStream fileStream = File.Create(path);

            using StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8);

            Save(path, writer, useFolders, collapseFolders, alwaysBuild);
        }

        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="rootPath">A root path for the solution to make other paths relative to.</param>
        /// <param name="writer">The <see cref="TextWriter" /> to save the solution file to.</param>
        /// <param name="useFolders">Specifies if folders should be created.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <param name="alwaysBuild">An optional value indicating whether or not to always include the project in the build even if it has no matching configuration.</param>
        internal void Save(string rootPath, TextWriter writer, bool useFolders, bool collapseFolders = false, bool alwaysBuild = true)
        {
            writer.WriteLine(Header, _fileFormatVersion);

            if (VisualStudioVersion != null)
            {
                writer.WriteLine($"# Visual Studio Version {VisualStudioVersion.Major}");
                writer.WriteLine($"VisualStudioVersion = {VisualStudioVersion}");
                writer.WriteLine($"MinimumVisualStudioVersion = {MinumumVisualStudioVersion}");
            }

            if (SolutionItems.Count > 0)
            {
                writer.WriteLine($@"Project(""{SlnFolder.FolderProjectTypeGuidString}"") = ""Solution Items"", ""Solution Items"", ""{{B283EBC2-E01F-412D-9339-FD56EF114549}}"" ");
                writer.WriteLine("	ProjectSection(SolutionItems) = preProject");
                foreach (string solutionItem in SolutionItems.Select(i => i.ToRelativePath(rootPath).ToSolutionPath()).Where(i => !string.IsNullOrWhiteSpace(i)))
                {
                    writer.WriteLine($"		{solutionItem} = {solutionItem}");
                }

                writer.WriteLine("	EndProjectSection");
                writer.WriteLine("EndProject");
            }

            List<SlnProject> sortedProjects = _projects.OrderBy(i => i.IsMainProject ? 0 : 1).ThenBy(i => i.FullPath).ToList();

            foreach (SlnProject project in sortedProjects)
            {
                string solutionPath = project.FullPath.ToRelativePath(rootPath).ToSolutionPath();

                if (ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(solutionPath, out Guid existingProjectGuid))
                {
                    project.ProjectGuid = existingProjectGuid;
                }

                writer.WriteLine($@"Project(""{project.ProjectTypeGuid.ToSolutionString()}"") = ""{project.Name}"", ""{solutionPath}"", ""{project.ProjectGuid.ToSolutionString()}""");
                writer.WriteLine("EndProject");
            }

            SlnHierarchy hierarchy = null;

            if (useFolders && sortedProjects.Any(i => !i.IsMainProject))
            {
                hierarchy = SlnHierarchy.CreateFromProjectDirectories(sortedProjects, collapseFolders);
            }
            else if (sortedProjects.Any(i => !string.IsNullOrWhiteSpace(i.SolutionFolder)))
            {
                hierarchy = SlnHierarchy.CreateFromProjectSolutionFolder(sortedProjects);
            }

            if (hierarchy != null)
            {
                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    string projectSolutionPath = (useFolders ? folder.FullPath.ToRelativePath(rootPath) : folder.Name).ToSolutionPath();

                    // Try to preserve the folder GUID if a matching relative folder path was parsed from an existing solution
                    if (ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(projectSolutionPath, out Guid projectGuid))
                    {
                        folder.FolderGuid = projectGuid;
                    }

                    writer.WriteLine($@"Project(""{folder.ProjectTypeGuidString}"") = ""{folder.Name}"", ""{projectSolutionPath}"", ""{folder.FolderGuid.ToSolutionString()}""");
                    writer.WriteLine("EndProject");
                }
            }

            writer.WriteLine("Global");

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");

            HashSet<string> solutionPlatforms = Platforms != null && Platforms.Any()
                ? new HashSet<string>(GetValidSolutionPlatforms(Platforms), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(GetValidSolutionPlatforms(sortedProjects.SelectMany(i => i.Platforms)), StringComparer.OrdinalIgnoreCase);

            HashSet<string> solutionConfigurations = Configurations != null && Configurations.Any()
                ? new HashSet<string>(Configurations, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(sortedProjects.SelectMany(i => i.Configurations).Where(i => !i.IsNullOrWhiteSpace()), StringComparer.OrdinalIgnoreCase);

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

            bool hasSharedProject = false;

            foreach (SlnProject project in sortedProjects)
            {
                if (project.IsSharedProject)
                {
                    hasSharedProject = true;
                    continue;
                }

                string projectGuid = project.ProjectGuid.ToSolutionString();

                foreach (string configuration in solutionConfigurations)
                {
                    bool foundConfiguration = TryGetProjectSolutionConfiguration(configuration, project, alwaysBuild, out string projectSolutionConfiguration);

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

            if (hierarchy != null)
            {
                writer.WriteLine(@"	GlobalSection(NestedProjects) = preSolution");

                foreach (SlnFolder folder in hierarchy.Folders.Where(i => i.Parent != null))
                {
                    foreach (SlnProject project in folder.Projects)
                    {
                        writer.WriteLine($@"		{project.ProjectGuid.ToSolutionString()} = {folder.FolderGuid.ToSolutionString()}");
                    }

                    if (useFolders)
                    {
                        writer.WriteLine($@"		{folder.FolderGuid.ToSolutionString()} = {folder.Parent.FolderGuid.ToSolutionString()}");
                    }
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("	GlobalSection(ExtensibilityGlobals) = postSolution");
            writer.WriteLine($"		SolutionGuid = {SolutionGuid.ToSolutionString()}");
            writer.WriteLine("	EndGlobalSection");

            if (hasSharedProject)
            {
                writer.WriteLine("	GlobalSection(SharedMSBuildProjectFiles) = preSolution");

                foreach (SlnProject project in sortedProjects)
                {
                    foreach (string sharedProjectItem in project.SharedProjectItems)
                    {
                        writer.WriteLine($"		{sharedProjectItem.ToRelativePath(rootPath).ToSolutionPath()}*{project.ProjectGuid.ToSolutionString(uppercase: false).ToLowerInvariant()}*SharedItemsImports = {GetSharedProjectOptions(project)}");
                    }
                }

                writer.WriteLine("	EndGlobalSection");
            }

            writer.WriteLine("EndGlobal");
        }

        private string GetSharedProjectOptions(SlnProject project)
        {
            if (project.FullPath.EndsWith(ProjectFileExtensions.VcxItems))
            {
                return "9";
            }

            if (project.FullPath.EndsWith(ProjectFileExtensions.Shproj))
            {
                return "13";
            }

            return "4";
        }

        private IEnumerable<string> GetValidSolutionPlatforms(IEnumerable<string> platforms)
        {
            List<string> values = platforms
                .Select(i => i.ToSolutionPlatform())
                .Select(platform =>
                {
                    return platform.ToLowerInvariant() switch
                    {
                        "any cpu" => platform,
                        "x64" => platform,
                        "x86" => platform,
                        "amd64" => "x64",
                        "win32" => "x86",
                        "arm" => platform,
                        "arm64" => platform,
                        _ => null
                    };
                })
                .Where(i => i != null)
                .OrderBy(i => i)
                .ToList();

            return values.Any() ? values : new List<string> { "Any CPU" };
        }

        private bool TryGetProjectSolutionConfiguration(string solutionConfiguration, SlnProject project, bool alwaysBuild, out string projectSolutionConfiguration)
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

            return alwaysBuild;
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
            bool containsArm = false;
            bool containsArm64 = false;

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

                    case "arm":
                        containsArm = true;
                        break;

                    case "arm64":
                        containsArm64 = true;
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

                if (containsArm)
                {
                    projectSolutionPlatform = projectBuildPlatform = "ARM";

                    return true;
                }

                if (containsArm64)
                {
                    projectSolutionPlatform = projectBuildPlatform = "ARM64";

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