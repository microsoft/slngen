// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a Visual Studio solution file.
    /// </summary>
    public sealed class SlnFile
    {
        private static readonly char[] DirectorySeparatorCharacters = new char[] { Path.DirectorySeparatorChar };

        /// <summary>
        /// Gets the projects.
        /// </summary>
        private readonly List<SlnProject> _projects = new ();

        /// <summary>
        /// A list of absolute paths to include as Solution Items.
        /// </summary>
        private readonly Dictionary<string, SlnItem> _solutionItems = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFile" /> class.
        /// </summary>
        public SlnFile()
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
        /// Gets or sets an optional minimum Visual Studio version for the solution file.
        /// </summary>
        public string MinimumVisualStudioVersion { get; set; } = "10.0.40219.1";

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
        public IReadOnlyDictionary<string, IReadOnlyCollection<string>> SolutionItems => _solutionItems.ToDictionary(
            k => k.Key,
            v => (IReadOnlyCollection<string>)v.Value.SolutionItems.AsReadOnly());

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

            IReadOnlyCollection<string> solutionItems = SlnProject.GetSolutionItems(projectList, logger).ToList();

            string solutionFileFullPath = arguments.SolutionFileFullPath?.LastOrDefault();

            if (solutionFileFullPath.IsNullOrWhiteSpace())
            {
                string solutionDirectoryFullPath = arguments.SolutionDirectoryFullPath?.LastOrDefault();

                if (solutionDirectoryFullPath.IsNullOrWhiteSpace())
                {
                    solutionDirectoryFullPath = firstProject.DirectoryPath;
                }

                var firstProjectName = firstProject.GetPropertyValueOrDefault(MSBuildPropertyNames.SlnGenProjectName, Path.GetFileName(firstProject.FullPath));

                string solutionFileName = Path.ChangeExtension(firstProjectName, "sln");

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

            SlnFile slnFile = new ()
            {
                Platforms = arguments.GetPlatforms(),
                Configurations = arguments.GetConfigurations(),
            };

            if (arguments.VisualStudioVersion.HasValue)
            {
                if (arguments.VisualStudioVersion.Version != null && Version.TryParse(arguments.VisualStudioVersion.Version, out Version version))
                {
                    slnFile.VisualStudioVersion = version;
                }

                if (slnFile.VisualStudioVersion == null)
                {
                    string devEnvFullPath = arguments.GetDevEnvFullPath(Program.CurrentDevelopmentEnvironment.VisualStudio);

                    if (!devEnvFullPath.IsNullOrWhiteSpace() && File.Exists(devEnvFullPath))
                    {
                        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(devEnvFullPath);

                        slnFile.VisualStudioVersion = new Version(fileVersionInfo.ProductMajorPart, fileVersionInfo.ProductMinorPart, fileVersionInfo.ProductBuildPart, fileVersionInfo.FilePrivatePart);
                    }
                }
            }

            if (TryParseExistingSolution(solutionFileFullPath, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath, out ISolutionSerializer serializer))
            {
                logger.LogMessageNormal("Updating existing solution file and reusing Visual Studio cache");
                slnFile.SolutionGuid = solutionGuid;
                slnFile.ExistingProjectGuids = projectGuidsByPath;
                arguments.LoadProjectsInVisualStudio = new[] { bool.TrueString };
            }

            bool isBuildable = true;
            if (arguments.GetGlobalProperties().TryGetValue(MSBuildPropertyNames.SlnGenIsBuildable, out string isBuildableString))
            {
                isBuildable = bool.TrueString.Equals(isBuildableString, StringComparison.OrdinalIgnoreCase);
            }

            slnFile.AddProjects(projectList, customProjectTypeGuids, arguments.IgnoreMainProject ? null : firstProject.FullPath, isBuildable);

            slnFile.AddSolutionItems(solutionItems);

            string slnGenFoldersPropertyValue = firstProject.GetPropertyValueOrDefault(MSBuildPropertyNames.SlnGenFolders, "false");
            var enableFolders = arguments.EnableFolders(slnGenFoldersPropertyValue);

            if (!logger.HasLoggedErrors)
            {
                slnFile.CreateSolutionDirectory(solutionFileFullPath);
                slnFile.Save(serializer, solutionFileFullPath, enableFolders, logger, arguments.EnableCollapseFolders(), arguments.EnableAlwaysBuild());
            }

            return (solutionFileFullPath, customProjectTypeGuids.Count, solutionItems.Count, solutionGuid);
        }

        /// <summary>
        /// Attempts to read the existing GUID from a solution file if one exists.
        /// </summary>
        /// <param name="solutionFileFullPath">Path to the existing solution file.</param>
        /// <param name="solutionGuid">Receives the <see cref="Guid" /> of the existing solution file if one is found, otherwise default(Guid).</param>
        /// <param name="projectGuidsByPath">Receives the project GUIDs by their full paths.</param>
        /// <param name="serializer">Serializer which can reads and determines the appropriate solution model from the solution file (based on the moniker).</param>
        /// <returns>true if the solution file could be correctly parsed.</returns>
        public static bool TryParseExistingSolution(string solutionFileFullPath, out Guid solutionGuid, out IReadOnlyDictionary<string, Guid> projectGuidsByPath, out ISolutionSerializer serializer)
        {
            projectGuidsByPath = default;
            solutionGuid = default;
            serializer = SolutionSerializers.GetSerializerByMoniker(solutionFileFullPath);

            FileInfo fileInfo = new FileInfo(solutionFileFullPath);
            if (!fileInfo.Exists || fileInfo.Directory == null)
            {
                return false;
            }

            if (serializer is null)
            {
                return false;
            }

            bool foundSolutionGuid = false;

            try
            {
                SolutionModel existingSolution = serializer.OpenAsync(solutionFileFullPath, CancellationToken.None).Result;

                Dictionary<string, Guid> projectGuids = new (StringComparer.OrdinalIgnoreCase);
                foreach (SolutionProjectModel project in existingSolution.SolutionProjects)
                {
                    projectGuids[project.FilePath] = project.Id;
                }

                foreach (SolutionFolderModel folder in existingSolution.SolutionFolders)
                {
                    projectGuids[GetSolutionFolderPathWithForwardSlashes(folder.Path)] = folder.Id;
                }

                IEnumerable<SolutionPropertyBag> existingSlnProperties = existingSolution.GetSlnProperties();
                SolutionPropertyBag extensibilityGlobals = existingSlnProperties.Where(x => x.Id == "ExtensibilityGlobals").FirstOrDefault();
                if (extensibilityGlobals is not null)
                {
                    extensibilityGlobals.TryGetValue("SolutionGuid", out string solutionGuidStr);
                    foundSolutionGuid = Guid.TryParse(solutionGuidStr, out solutionGuid);
                }

                projectGuidsByPath = projectGuids;
            }
            catch (SolutionException)
            {
                // There was an unrecoverable syntax error reading the solution file.
                return false;
            }

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
        /// <param name="isBuildable">Indicates whether the projects are buildable.</param>
        public void AddProjects(IEnumerable<Project> projects, IReadOnlyDictionary<string, Guid> customProjectTypeGuids, string mainProjectFullPath = null, bool isBuildable = true)
        {
            _projects.AddRange(
                projects
                    .Distinct(new EqualityComparer<Project>((x, y) => string.Equals(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase), i => i.FullPath.GetHashCode()))
                    .Select(i => SlnProject.FromProject(i, customProjectTypeGuids, string.Equals(i.FullPath, mainProjectFullPath, StringComparison.OrdinalIgnoreCase), isBuildable))
                    .Where(i => i != null));
        }

        /// <summary>
        /// Adds the specified solution items.
        /// </summary>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(IEnumerable<string> items)
        {
            AddSolutionItems("Solution Items", items);
        }

        /// <summary>
        /// Adds the specified solution items under the specified path.
        /// </summary>
        /// <param name="folderPath">The path the solution items will be added in.</param>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(string folderPath, IEnumerable<string> items)
        {
            AddSolutionItems(folderPath, new Guid("B283EBC2-E01F-412D-9339-FD56EF114549"), items);
        }

        /// <summary>
        /// Adds the specified solution items under the specified path.
        /// </summary>
        /// <param name="folderPath">The path the solution items will be added in.</param>
        /// <param name="folderGuid">The unique GUID for the folder.</param>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(string folderPath, Guid folderGuid, IEnumerable<string> items)
        {
            AddSolutionItems(null, folderPath, folderGuid, items);
        }

        /// <summary>
        /// Adds the specified solution items under the specified path.
        /// </summary>
        /// <param name="parentFolderGuid">The unique GUID for the parent folder.</param>
        /// <param name="folderPath">The path the solution items will be added in.</param>
        /// <param name="folderGuid">The unique GUID for the folder.</param>
        /// <param name="items">An <see cref="IEnumerable{String}"/> containing items to add to the solution.</param>
        public void AddSolutionItems(Guid? parentFolderGuid, string folderPath, Guid folderGuid, IEnumerable<string> items)
        {
            _solutionItems.Add(folderPath, new SlnItem(parentFolderGuid, folderGuid, items));
        }

        /// <summary>
        /// Creates the directory where the solution resides.
        /// </summary>
        /// <param name="rootPath">A root path for the solution.</param>
        internal void CreateSolutionDirectory(string rootPath)
        {
            string directoryName = Path.GetDirectoryName(rootPath);

            if (!directoryName.IsNullOrWhiteSpace())
            {
                Directory.CreateDirectory(directoryName!);
            }
        }

        /// <summary>
        /// Saves the Visual Studio solution to a file.
        /// </summary>
        /// <param name="serializer">Serializer which saves the solution model to the solution file (based on its moniker).</param>
        /// <param name="rootPath">A root path for the solution to make other paths relative to.</param>
        /// <param name="useFolders">Specifies if folders should be created.</param>
        /// <param name="logger">A <see cref="ISlnGenLogger" /> to use for logging.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <param name="alwaysBuild">An optional value indicating whether or not to always include the project in the build even if it has no matching configuration.</param>
        internal void Save(ISolutionSerializer serializer, string rootPath, bool useFolders, ISlnGenLogger logger = null, bool collapseFolders = false, bool alwaysBuild = true)
        {
            SolutionModel newSolution = new ();

            // Set UTF8 BOM encoding for .sln
            if (serializer is ISolutionSerializer<SlnV12SerializerSettings> v12Serializer)
            {
                newSolution.SerializerExtension = v12Serializer.CreateModelExtension(new ()
                {
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                });
            }

            if (VisualStudioVersion != null)
            {
                newSolution.VisualStudioProperties.OpenWith = $"Visual Studio Version {VisualStudioVersion.Major}";
                newSolution.VisualStudioProperties.Version = VisualStudioVersion;
                if (Version.TryParse(MinimumVisualStudioVersion, out var minimumVisualStudioVersion))
                {
                    newSolution.VisualStudioProperties.MinimumVersion = minimumVisualStudioVersion;
                }
            }

            List<SlnProject> sortedProjects = _projects.OrderBy(i => i.IsMainProject ? 0 : 1).ThenBy(i => i.FullPath).ToList();
            foreach (SlnProject project in sortedProjects)
            {
                string solutionPath = project.FullPath.ToRelativePath(rootPath).ToSolutionPath();

                if (ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(solutionPath, out Guid existingProjectGuid))
                {
                    project.ProjectGuid = existingProjectGuid;
                }

                SolutionProjectModel projectModel = newSolution.AddProject(solutionPath, project.ProjectTypeGuid.ToSolutionString(), null);
                projectModel.DisplayName = project.Name;
                projectModel.Id = project.ProjectGuid;
            }

            SlnHierarchy hierarchy = null;

            if (useFolders && sortedProjects.Any(i => !i.IsMainProject))
            {
                hierarchy = SlnHierarchy.CreateFromProjectDirectories(sortedProjects, SolutionItems, collapseFolders);
            }
            else if (sortedProjects.Any(i => !string.IsNullOrWhiteSpace(i.SolutionFolder)))
            {
                hierarchy = SlnHierarchy.CreateFromProjectSolutionFolder(sortedProjects, SolutionItems);
            }
            else
            {
                // Just handle the solution items
                foreach (var solutionItems in _solutionItems)
                {
                    if (solutionItems.Value.SolutionItems.Any())
                    {
                        SolutionFolderModel newFolder = AddFolderToModel(newSolution, solutionItems.Key, solutionItems.Value.FolderGuid);
                        AddSolutionItemsToModel(newFolder, solutionItems.Value.SolutionItems, rootPath);
                    }
                }

                // Nest solution folders within their parent folders
                var solutionItemsWithParents = _solutionItems.Where(x => x.Value.ParentFolderGuid.HasValue).ToArray();
                if (solutionItemsWithParents.Length > 0)
                {
                    AddNestedProjectsToModel(newSolution, solutionItemsWithParents);
                }
            }

            if (hierarchy != null)
            {
                bool logDriveWarning = false;
                string rootPathDrive = Path.GetPathRoot(Path.GetFullPath(rootPath));
                foreach (SlnFolder folder in hierarchy.Folders)
                {
                    bool useSeparateDrive = false;
                    bool hasFullPath = !string.IsNullOrEmpty(folder.FullPath);
                    if (hasFullPath)
                    {
                        string folderPathDrive = Path.GetPathRoot(Path.GetFullPath(folder.FullPath));
                        // Only compare path roots when root path has root directory information
                        if (!string.IsNullOrEmpty(rootPathDrive) &&
                            rootPathDrive.Length == folderPathDrive.Length &&
                            !string.Equals(rootPathDrive, folderPathDrive, StringComparison.OrdinalIgnoreCase))
                        {
                            useSeparateDrive = true;
                            if (!logDriveWarning)
                            {
                                logger?.LogWarning($"Detected folder on a different drive from the root solution path {rootPath}. This folder should not be committed to source control since it does not contain a simple, relative path and is not guaranteed to work across machines.");
                                logDriveWarning = true;
                            }
                        }
                    }

                    string projectSolutionPath = (useFolders && !useSeparateDrive && hasFullPath ? folder.FullPath.ToRelativePath(rootPath) : folder.FullPath).ToSolutionPath();

                    // Try to preserve the folder GUID if a matching relative folder path was parsed from an existing solution
                    if (ExistingProjectGuids != null && ExistingProjectGuids.TryGetValue(projectSolutionPath, out Guid projectGuid))
                    {
                        folder.FolderGuid = projectGuid;
                    }

                    // guard against root folder
                    if (folder != hierarchy.RootFolder)
                    {
                        SolutionFolderModel newFolder = AddFolderToModel(newSolution, folder.Name, folder.FolderGuid);
                        if (folder.SolutionItems.Count > 0)
                        {
                            AddSolutionItemsToModel(newFolder, folder.SolutionItems, rootPath);
                        }
                    }
                    else if (folder.SolutionItems.Count > 0)
                    {
                        // Special case for solution items in root folder
                        SolutionFolderModel newFolder = AddFolderToModel(newSolution, "Solution Items", new Guid("B283EBC2-E01F-412D-9339-FD56EF114549"));
                        AddSolutionItemsToModel(newFolder, folder.SolutionItems, rootPath);
                    }
                }

                AddHierarchyNestedProjectsToModel(newSolution, hierarchy);
            }

            HashSet<string> solutionPlatforms = Platforms != null && Platforms.Any()
                ? new HashSet<string>(GetValidSolutionPlatforms(Platforms), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(GetValidSolutionPlatforms(sortedProjects.SelectMany(i => i.Platforms)), StringComparer.OrdinalIgnoreCase);

            HashSet<string> solutionConfigurations = Configurations != null && Configurations.Any()
                ? new HashSet<string>(Configurations, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(sortedProjects.SelectMany(i => i.Configurations).Where(i => !i.IsNullOrWhiteSpace()), StringComparer.OrdinalIgnoreCase);

            AddSolutionConfigurationPlatformsToModel(newSolution, solutionConfigurations, solutionPlatforms);

            bool hasSharedProject = AddProjectConfigurationPlatformsToModel(newSolution, sortedProjects, solutionConfigurations, solutionPlatforms, alwaysBuild);

            if (hasSharedProject)
            {
                AddSharedMSBuildProjectFilesToModel(newSolution, sortedProjects, rootPath);
            }

            AddSolutionGuidToModel(newSolution);

            serializer.SaveAsync(rootPath, newSolution, CancellationToken.None).Wait();
        }

        private static string GetSolutionFolderPathWithForwardSlashes(string path)
        {
            // SolutionModel::AddFolder expects paths to have leading, trailing and inner forward slashes
            // https://github.com/microsoft/vs-solutionpersistence/blob/87ee8ea069662d55c336a9bd68fe4851d0384fa5/src/Microsoft.VisualStudio.SolutionPersistence/Model/SolutionModel.cs#L171C1-L172C1
            return "/" + string.Join("/", GetPathWithDirectorySeparator(path).Split(DirectorySeparatorCharacters, StringSplitOptions.RemoveEmptyEntries)) + "/";
        }

        private static string GetPathWithDirectorySeparator(string path) => path.Replace('\\', '/');

        private static SolutionFolderModel AddFolderToModel(SolutionModel newSolution, string solutionFolder, Guid folderGuid)
        {
            SolutionFolderModel solutionFolderModel = newSolution.AddFolder(GetSolutionFolderPathWithForwardSlashes(solutionFolder));
            solutionFolderModel.Id = folderGuid;
            return solutionFolderModel;
        }

        private static void AddSolutionItemsToModel(SolutionFolderModel newFolder, IEnumerable<string> solutionItems, string rootPath)
        {
            SolutionPropertyBag slnProperties = new ("SolutionItems", scope: PropertiesScope.PreLoad);
            foreach (string solutionItem in solutionItems
                .Select(i => i.ToRelativePath(rootPath).ToSolutionPath())
                .Where(i => !string.IsNullOrWhiteSpace(i)))
            {
                slnProperties.Add(solutionItem, solutionItem);
            }

            newFolder.AddSlnProperties(slnProperties);
        }

        private static void AddNestedProjectsToModel(SolutionModel newSolution, KeyValuePair<string, SlnItem>[] solutionItemsWithParents)
        {
            SolutionPropertyBag slnProperties = new ("NestedProjects", scope: PropertiesScope.PreLoad);
            foreach (KeyValuePair<string, SlnItem> solutionItem in solutionItemsWithParents)
            {
                slnProperties.Add(solutionItem.Value.FolderGuid.ToSolutionString(), solutionItem.Value.ParentFolderGuid.Value.ToSolutionString());
            }

            newSolution.AddSlnProperties(slnProperties);
        }

        private static void AddHierarchyNestedProjectsToModel(SolutionModel newSolution, SlnHierarchy hierarchy)
        {
            var foldersWithParents = hierarchy.Folders.Where(i => i.Parent != null).ToArray();
            if (foldersWithParents.Length > 0)
            {
                SolutionPropertyBag slnProperties = new ("NestedProjects", scope: PropertiesScope.PreLoad);
                foreach (SlnFolder folder in foldersWithParents)
                {
                    foreach (SlnProject project in folder.Projects)
                    {
                        slnProperties.Add(project.ProjectGuid.ToSolutionString(), folder.FolderGuid.ToSolutionString());
                    }

                    // guard against root folder
                    if (folder.Parent != hierarchy.RootFolder)
                    {
                        slnProperties.Add(folder.FolderGuid.ToSolutionString(), folder.Parent.FolderGuid.ToSolutionString());
                    }
                }

                newSolution.AddSlnProperties(slnProperties);
            }
        }

        private void AddSharedMSBuildProjectFilesToModel(SolutionModel newSolution, List<SlnProject> sortedProjects, string rootPath)
        {
            SolutionPropertyBag slnProperties = new ("SharedMSBuildProjectFiles", scope: PropertiesScope.PreLoad);
            foreach (SlnProject project in sortedProjects)
            {
                foreach (string sharedProjectItem in project.SharedProjectItems)
                {
                    slnProperties.Add($"{sharedProjectItem.ToRelativePath(rootPath).ToSolutionPath()}*{project.ProjectGuid.ToSolutionString(uppercase: false).ToLowerInvariant()}*SharedItemsImports", $"{GetSharedProjectOptions(project)}");
                }
            }

            newSolution.AddSlnProperties(slnProperties);
        }

        private bool AddProjectConfigurationPlatformsToModel(SolutionModel newSolution, List<SlnProject> sortedProjects, HashSet<string> solutionConfigurations, HashSet<string> solutionPlatforms, bool alwaysBuild)
        {
            bool hasSharedProject = false;

            SolutionPropertyBag slnProperties = new ("ProjectConfigurationPlatforms", scope: PropertiesScope.PostLoad);

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

                        slnProperties.Add($"{projectGuid}.{configuration}|{platform}.ActiveCfg", $"{projectSolutionConfiguration}|{projectSolutionPlatform}");

                        if (foundPlatform && foundConfiguration && project.IsBuildable)
                        {
                            slnProperties.Add($"{projectGuid}.{configuration}|{platform}.Build.0", $"{projectSolutionConfiguration}|{projectBuildPlatform}");
                        }

                        if (project.IsDeployable)
                        {
                            slnProperties.Add($"{projectGuid}.{configuration}|{platform}.Deploy.0", $"{projectSolutionConfiguration}|{projectSolutionPlatform}");
                        }
                    }
                }
            }

            newSolution.AddSlnProperties(slnProperties);

            return hasSharedProject;
        }

        private void AddSolutionConfigurationPlatformsToModel(SolutionModel newSolution, HashSet<string> solutionConfigurations, HashSet<string> solutionPlatforms)
        {
            SolutionPropertyBag slnProperties = new ("SolutionConfigurationPlatforms", scope: PropertiesScope.PreLoad);
            foreach (string configuration in solutionConfigurations)
            {
                foreach (string platform in solutionPlatforms)
                {
                    if (!string.IsNullOrWhiteSpace(configuration) && !string.IsNullOrWhiteSpace(platform))
                    {
                        slnProperties.Add($"{configuration}|{platform}", $"{configuration}|{platform}");
                    }
                }
            }

            newSolution.AddSlnProperties(slnProperties);
        }

        private void AddSolutionGuidToModel(SolutionModel newSolution)
        {
            SolutionPropertyBag newExtensibilityGlobals = new ("ExtensibilityGlobals")
            {
                { "SolutionGuid", SolutionGuid.ToString() },
            };

            newSolution.AddSlnProperties(newExtensibilityGlobals);
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