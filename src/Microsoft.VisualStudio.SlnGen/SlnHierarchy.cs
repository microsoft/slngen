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
        /// Character used for separating collapsed folders
        /// </summary>
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
        /// Creates a <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.
        /// </summary>
        /// <param name="projects">The set of projects that should be placed in the hierarchy.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        /// <returns>A <see cref="SlnHierarchy" /> based on the directory structure of the specified projects.</returns>
        public static SlnHierarchy CreateFromProjectDirectories(IReadOnlyList<SlnProject> projects, bool collapseFolders = false)
        {
            SlnFolder rootFolder = GetRootFolder(projects);

            SlnHierarchy hierarchy = new SlnHierarchy(rootFolder);

            foreach (SlnProject project in projects.Where(i => !i.IsMainProject))
            {
                CreateHierarchy(hierarchy, project);
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
        /// <returns>A <see cref="SlnHierarchy" /> object containing solution folders and projects.</returns>
        public static SlnHierarchy CreateFromProjectSolutionFolder(IReadOnlyList<SlnProject> projects)
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

            return hierarchy;
        }

        private static void CollapseFolders(IReadOnlyCollection<SlnFolder> folders)
        {
            foreach (SlnFolder folder in folders)
            {
                CollapseFolders(folder.Folders);
            }

            foreach (SlnFolder folderWithSingleChild in folders.Where(i => i.Parent != null && i.Folders.Count == 1 && i.Projects.Count == 0))
            {
                SlnFolder child = folderWithSingleChild.Folders.First();

                folderWithSingleChild.Name = $"{folderWithSingleChild.Name} {Separator} {child.Name}";
                folderWithSingleChild.Projects.AddRange(child.Projects);
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

        private static SlnFolder GetRootFolder(IEnumerable<SlnProject> projects)
        {
            List<string> paths = projects.Where(i => !i.IsMainProject).Select(i => Directory.GetParent(i.FullPath).FullName).ToList();

            if (!paths.Any())
            {
                throw new InvalidOperationException();
            }

            // TODO: Unit tests, optimize
            string commonPath = string.Empty;

            List<string> separatedPath = paths
                .First(str => str.Length == paths.Max(st2 => st2.Length))
                .Split(new[] { Path.DirectorySeparatorChar })
                .ToList();

            string nextPath = null;
            foreach (string pathSegment in separatedPath.AsEnumerable())
            {
                if (commonPath.Length == 0 && paths.All(str => str.StartsWith(pathSegment, StringComparison.OrdinalIgnoreCase)))
                {
                    commonPath = pathSegment + Path.DirectorySeparatorChar;
                }
                else if (paths.All(str => str.StartsWith(nextPath = $"{commonPath}{pathSegment}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
                {
                    commonPath = nextPath;
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

            if (!string.IsNullOrWhiteSpace(folder.Name))
            {
                yield return folder;
            }
        }
    }
}