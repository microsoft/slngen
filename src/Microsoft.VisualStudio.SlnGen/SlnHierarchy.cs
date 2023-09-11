// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the hierarchy of projects in a Visual Studio solution.
    /// </summary>
    /// <remarks>This assumes all projects are on the same drive.</remarks>
    public sealed class SlnHierarchy
    {
        /// <summary>
        /// Character used for separating collapsed folders.
        /// </summary>
        /// <remarks>
        /// A special character is used because Solution Folder names cannot:
        ///  • contain any of the following characters: / ? : \ * "" < > |
        ///  • contain Unicode control characters
        ///  • contain surrogate characters
        ///  • be system reserved names, including 'CON', 'AUX', 'PRN', 'COM1' or 'LPT2'
        ///  • be '.' or '..'
        /// </remarks>
        public const char Separator = '\u0338';

        /// <summary>
        /// Stores a mapping of full paths to <see cref="SlnFolder" /> objects.
        /// </summary>
        private readonly Dictionary<string, SlnFolder> _pathToSlnFolderMap = new (StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores the root folder based on a common rooted path for all projects.
        /// </summary>
        private readonly SlnFolder _rootFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnHierarchy" /> class.
        /// </summary>
        private SlnHierarchy(SlnFolder rootFolder)
        {
            _rootFolder = rootFolder ?? throw new ArgumentNullException(nameof(rootFolder));
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{SlnFolder}" /> containing the current folders.
        /// </summary>
        public IEnumerable<SlnFolder> Folders => EnumerateFolders(_rootFolder);

        /// <summary>
        /// Gets the root folder.
        /// </summary>
        public SlnFolder RootFolder => _rootFolder;

        /// <summary>
        /// Creates a <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.
        /// </summary>
        /// <param name="projects">The set of projects that should be placed in the hierarchy.</param>
        /// <param name="solutionItems">The set of solution items that should be placed in the hierarchy.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <returns>A <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.</returns>
        public static SlnHierarchy CreateFromProjectDirectories(
            IReadOnlyList<SlnProject> projects,
            IReadOnlyCollection<string> solutionItems,
            bool collapseFolders = false)
        {
            return CreateFromProjectDirectories(
                projects,
                new Dictionary<string, IReadOnlyCollection<string>>
                {
                    { "Solution Items", solutionItems },
                },
                collapseFolders);
        }

        /// <summary>
        /// Creates a <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.
        /// </summary>
        /// <param name="projects">The set of projects that should be placed in the hierarchy.</param>
        /// <param name="solutionItems">The set of solution items that should be placed in the hierarchy.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <returns>A <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.</returns>
        public static SlnHierarchy CreateFromProjectDirectories(
            IReadOnlyList<SlnProject> projects,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> solutionItems,
            bool collapseFolders = false)
        {
            SlnFolder rootFolder = GetRootFolder(projects, solutionItems.Values.SelectMany(x => x));

            SlnHierarchy hierarchy = new SlnHierarchy(rootFolder)
            {
                _pathToSlnFolderMap =
                {
                    [rootFolder.FullPath] = rootFolder,
                },
            };

            foreach (SlnProject project in projects.Where(i => !i.IsMainProject))
            {
                CreateHierarchy(hierarchy, project);
            }

            foreach (var solutionItem in solutionItems)
            {
                CreateHierarchy(hierarchy, solutionItem);
            }

            if (collapseFolders)
            {
                RemoveLeaves(rootFolder);
                CollapseFolders(rootFolder.Folders);
            }

            return hierarchy;
        }

        /// <summary>
        /// Creates a hierarchy based on solution folders declared by projects.
        /// </summary>
        /// <param name="projects">A <see cref="IReadOnlyList{T}" /> of projects.</param>
        /// <param name="solutionItems">The set of solution items that should be placed in the hierarchy.</param>
        /// <returns>A <see cref="SlnHierarchy" /> object containing solution folders and projects.</returns>
        public static SlnHierarchy CreateFromProjectSolutionFolder(
            IReadOnlyList<SlnProject> projects,
            IReadOnlyCollection<string> solutionItems)
        {
            return CreateFromProjectSolutionFolder(
                projects,
                new Dictionary<string, IReadOnlyCollection<string>>
                {
                    { "Solution Items", solutionItems },
                });
        }

        /// <summary>
        /// Creates a hierarchy based on solution folders declared by projects.
        /// </summary>
        /// <param name="projects">A <see cref="IReadOnlyList{T}" /> of projects.</param>
        /// <param name="solutionItems">The set of solution items that should be placed in the hierarchy.</param>
        /// <returns>A <see cref="SlnHierarchy" /> object containing solution folders and projects.</returns>
        public static SlnHierarchy CreateFromProjectSolutionFolder(
            IReadOnlyList<SlnProject> projects,
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> solutionItems)
        {
            SlnHierarchy hierarchy = new SlnHierarchy(new SlnFolder(string.Empty));

            foreach (SlnProject project in projects.Where(i => !string.IsNullOrWhiteSpace(i.SolutionFolder)))
            {
                string[] nestedFolders = project.SolutionFolder.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < nestedFolders.Length; i++)
                {
                    string folderPath = string.Join(Path.DirectorySeparatorChar.ToString(), nestedFolders, 0, i + 1);

                    if (!hierarchy._pathToSlnFolderMap.TryGetValue(folderPath, out SlnFolder nested))
                    {
                        SlnFolder parent;

                        if (i == 0)
                        {
                            parent = hierarchy._rootFolder;
                        }
                        else
                        {
                            string parentString = Path.GetDirectoryName(folderPath);
                            parent = hierarchy._pathToSlnFolderMap[parentString];
                        }

                        nested = new SlnFolder(folderPath)
                        {
                            Parent = parent,
                        };

                        hierarchy._pathToSlnFolderMap.Add(folderPath, nested);

                        parent.Folders.Add(nested);
                    }

                    if (i == nestedFolders.Length - 1)
                    {
                        nested.Projects.Add(project);
                    }
                }
            }

            foreach (var solutionItem in solutionItems)
            {
                string folderPath = solutionItem.Key;
                IReadOnlyCollection<string> items = solutionItem.Value;

                // try to get the existing folder in the solution
                if (hierarchy._pathToSlnFolderMap.TryGetValue(folderPath, out SlnFolder targetFolder))
                {
                    // the solution folder already exists
                    // add the solution items to the folder
                    targetFolder.SolutionItems.AddRange(items);
                }
                else
                {
                    // the solution folder doesn't already exist
                    // create the solution folder by creating the parent folder if it don't already exist
                    SlnFolder parent = null;

                    // get the folder name segments of the folder path
                    string[] folderSegments = folderPath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                    // iterate through each folder segment
                    for (int i = 0; i < folderSegments.Length; i++)
                    {
                        string nestedFolderPath = string.Join(Path.DirectorySeparatorChar.ToString(), folderSegments, 0, i + 1);

                        // try to get the existing folder in the solution
                        if (!hierarchy._pathToSlnFolderMap.TryGetValue(nestedFolderPath, out SlnFolder nested))
                        {
                            // the solution folder doesn't already exist
                            // in order to create the folder, we need to get the parent
                            if (i == 0)
                            {
                                // the parent is root folder
                                parent = hierarchy._rootFolder;
                            }
                            else
                            {
                                string parentString = Path.GetDirectoryName(nestedFolderPath);
                                parent = hierarchy._pathToSlnFolderMap[parentString];
                            }

                            // create the folder
                            nested = new SlnFolder(nestedFolderPath)
                            {
                                Parent = parent,
                            };

                            // add the folder to the folder map
                            hierarchy._pathToSlnFolderMap.Add(nestedFolderPath, nested);
                        }
                        else
                        {
                            parent = nested;
                        }
                    }

                    // add the solution items to the folder
                    parent.SolutionItems.AddRange(items);
                }
            }

            return hierarchy;
        }

        private static void CollapseFolders(IReadOnlyCollection<SlnFolder> folders)
        {
            foreach (SlnFolder folder in folders)
            {
                CollapseFolders(folder.Folders);
            }

            foreach (SlnFolder folderWithSingleChild in folders.Where(i =>
                         i.Parent != null && i.Folders.Count == 1 && i.Projects.Count == 0 &&
                         i.SolutionItems.Count == 0))
            {
                SlnFolder child = folderWithSingleChild.Folders.First();

                folderWithSingleChild.Name = $"{folderWithSingleChild.Name} {Separator} {child.Name}";
                folderWithSingleChild.Projects.AddRange(child.Projects);
                folderWithSingleChild.SolutionItems.AddRange(child.SolutionItems);
                folderWithSingleChild.Folders.Clear();
                folderWithSingleChild.Folders.AddRange(child.Folders);
                folderWithSingleChild.Folders.ForEach(f => f.Parent = folderWithSingleChild);
            }
        }

        private static void CreateHierarchy(SlnHierarchy hierarchy, SlnProject project)
        {
            FileInfo fileInfo = new FileInfo(project.FullPath);

            DirectoryInfo directoryInfo = fileInfo.Directory;

            if (hierarchy._pathToSlnFolderMap.TryGetValue(directoryInfo!.FullName, out SlnFolder childFolder))
            {
                childFolder.Projects.Add(project);

                return;
            }

            childFolder = new SlnFolder(directoryInfo.FullName);

            childFolder.Projects.Add(project);

            hierarchy._pathToSlnFolderMap.Add(directoryInfo.FullName, childFolder);

            directoryInfo = directoryInfo.Parent;

            if (directoryInfo != null)
            {
                while (directoryInfo != null && !string.Equals(directoryInfo.FullName, hierarchy._rootFolder.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!hierarchy._pathToSlnFolderMap.TryGetValue(directoryInfo.FullName, out SlnFolder folder1))
                    {
                        folder1 = new SlnFolder(directoryInfo.FullName);
                        hierarchy._pathToSlnFolderMap.Add(directoryInfo.FullName, folder1);
                    }

                    childFolder.Parent = folder1;

                    if (!folder1.Folders.Contains(childFolder))
                    {
                        folder1.Folders.Add(childFolder);
                    }

                    directoryInfo = directoryInfo.Parent;

                    childFolder = folder1;
                }

                if (!hierarchy._rootFolder.Folders.Contains(childFolder))
                {
                    hierarchy._rootFolder.Folders.Add(childFolder);
                    childFolder.Parent = hierarchy._rootFolder;
                }
            }
        }

        private static void CreateHierarchy(SlnHierarchy hierarchy, KeyValuePair<string, IReadOnlyCollection<string>> solutionItems)
        {
            foreach (string solutionItem in solutionItems.Value)
            {
                FileInfo fileInfo = new FileInfo(solutionItem);
                DirectoryInfo directoryInfo = fileInfo.Directory;
                if (hierarchy._pathToSlnFolderMap.TryGetValue(directoryInfo!.FullName, out SlnFolder childFolder))
                {
                    childFolder.SolutionItems.Add(solutionItem);
                    return;
                }

                childFolder = new SlnFolder(directoryInfo.FullName);
                childFolder.SolutionItems.Add(solutionItem);
                hierarchy._pathToSlnFolderMap.Add(directoryInfo.FullName, childFolder);

                directoryInfo = directoryInfo.Parent;
                if (directoryInfo != null)
                {
                    while (directoryInfo != null && !string.Equals(directoryInfo.FullName, hierarchy._rootFolder.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!hierarchy._pathToSlnFolderMap.TryGetValue(directoryInfo.FullName, out SlnFolder folder1))
                        {
                            folder1 = new SlnFolder(directoryInfo.FullName);
                            hierarchy._pathToSlnFolderMap.Add(directoryInfo.FullName, folder1);
                        }

                        if (!folder1.Folders.Contains(childFolder))
                        {
                            folder1.Folders.Add(childFolder);
                            childFolder.Parent = folder1;
                        }

                        directoryInfo = directoryInfo.Parent;
                        childFolder = folder1;
                    }

                    if (!hierarchy._rootFolder.Folders.Contains(childFolder))
                    {
                        hierarchy._rootFolder.Folders.Add(childFolder);
                        childFolder.Parent = hierarchy._rootFolder;
                    }
                }
            }
        }

        private static SlnFolder GetRootFolder(
            IEnumerable<SlnProject> projects,
            IEnumerable<string> solutionItems)
        {
            List<string> paths = projects.Where(i => !i.IsMainProject)
                .Select(i => Directory.GetParent(i.FullPath)?.FullName ?? string.Empty)
                .Concat(solutionItems.Select(i => Directory.GetParent(i)?.FullName ?? string.Empty)).ToList();

            if (!paths.Any())
            {
                throw new InvalidOperationException();
            }

            // TODO: Unit tests, optimize
            string commonPath = string.Empty;

            int pathsMaxLength = paths.Max(str => str.Length);
            List<string> separatedPath = paths
                .First(str => str.Length == pathsMaxLength)
                .Split(new[] { Path.DirectorySeparatorChar })
                .ToList();

            string nextPath = null;
            foreach (string pathSegment in separatedPath.AsEnumerable())
            {
                if (commonPath.Length == 0 && paths.All(str => str.StartsWith(pathSegment, StringComparison.OrdinalIgnoreCase)))
                {
                    commonPath = pathSegment + Path.DirectorySeparatorChar;
                }
                else if (paths.All(str => str.StartsWith(nextPath = $"{commonPath}{pathSegment}", StringComparison.OrdinalIgnoreCase)))
                {
                    commonPath = nextPath + Path.DirectorySeparatorChar;
                }
                else
                {
                    break;
                }
            }

            return new SlnFolder(commonPath.TrimEnd(Path.DirectorySeparatorChar));
        }

        private static bool RemoveLeaves(SlnFolder folder)
        {
            // Check if we are a leaf node
            if (folder.Folders.Count == 0)
            {
                // We want to remove leaves that have only a single project. There is no need to keep that
                // directory as it would be represented by the project file itself.
                return folder.Projects.Count == 1;
            }

            List<SlnFolder> foldersToRemove = new List<SlnFolder>();
            foreach (SlnFolder child in folder.Folders)
            {
                if (RemoveLeaves(child))
                {
                    folder.Projects.AddRange(child.Projects);
                    folder.SolutionItems.AddRange(child.SolutionItems);
                    foldersToRemove.Add(child);
                }
            }

            foreach (SlnFolder folderToRemove in foldersToRemove)
            {
                folder.Folders.Remove(folderToRemove);
            }

            return false;
        }

        private IEnumerable<SlnFolder> EnumerateFolders(SlnFolder folder)
        {
            foreach (SlnFolder child in folder.Folders)
            {
                foreach (SlnFolder enumerateFolder in EnumerateFolders(child).Where(i => !string.IsNullOrWhiteSpace(i.Name)))
                {
                    yield return enumerateFolder;
                }
            }

            yield return folder;
        }
    }
}