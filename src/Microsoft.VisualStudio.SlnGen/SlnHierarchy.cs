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
        private readonly Dictionary<string, SlnFolder> _pathToSlnFolderMap = new Dictionary<string, SlnFolder>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores the root folder based on a common rooted path for all projects.
        /// </summary>
        private readonly SlnFolder _rootFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnHierarchy" /> class.
        /// </summary>
        /// <param name="projects">The set of projects that should be placed in the hierarchy.</param>
        /// <param name="collapseFolders">An optional value indicating whether or not folders containing a single item should be collapsed into their parent folder.</param>
        public SlnHierarchy(IReadOnlyList<SlnProject> projects, bool collapseFolders = false)
        {
            _rootFolder = GetRootFolder(projects);

            foreach (SlnProject project in projects.Where(i => !i.IsMainProject))
            {
                CreateHierarchy(project);
            }

            if (collapseFolders)
            {
                CollapseFolders(_rootFolder.Folders);
                RemoveLeaves(_rootFolder);
            }
        }

        public IEnumerable<SlnFolder> Folders => EnumerateFolders(_rootFolder);

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
                .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
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

        private static void CollapseFolders(IReadOnlyCollection<SlnFolder> folders)
        {
            foreach (SlnFolder folder in folders)
            {
                CollapseFolders(folder.Folders);
            }

            foreach (SlnFolder folderWithSingleChild in folders.Where(i => i.Folders.Count == 1))
            {
                SlnFolder child = folderWithSingleChild.Folders.First();

                folderWithSingleChild.Name = $"{folderWithSingleChild.Name} {Separator} {child.Name}";
                folderWithSingleChild.Projects.AddRange(child.Projects);
                folderWithSingleChild.Folders.Clear();
                folderWithSingleChild.Folders.AddRange(child.Folders);
            }
        }

        private static bool RemoveLeaves(SlnFolder folder)
        {
            // Check if we are a leave node
            if (folder.Folders.Count == 0)
            {
                // We want to remove leaves that have only a single project. There is no need to keep that
                // directory as it would be represented by the project file itself. We ignore folders that
                // contain the Separator character as these are folders which have been collapsed already.
                return folder.Projects.Count == 1 && !folder.Name.Contains(Separator);
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

        private void CreateHierarchy(SlnProject project)
        {
            FileInfo fileInfo = new FileInfo(project.FullPath);

            DirectoryInfo directoryInfo = fileInfo.Directory;

            if (_pathToSlnFolderMap.TryGetValue(directoryInfo.FullName, out SlnFolder childFolder))
            {
                childFolder.Projects.Add(project);

                return;
            }

            childFolder = new SlnFolder(directoryInfo.FullName);

            childFolder.Projects.Add(project);

            _pathToSlnFolderMap.Add(directoryInfo.FullName, childFolder);

            directoryInfo = directoryInfo.Parent;

            while (!string.Equals(directoryInfo.FullName, _rootFolder.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                if (!_pathToSlnFolderMap.TryGetValue(directoryInfo.FullName, out SlnFolder folder1))
                {
                    folder1 = new SlnFolder(directoryInfo.FullName);
                    _pathToSlnFolderMap.Add(directoryInfo.FullName, folder1);
                }

                childFolder.Parent = folder1;

                if (!folder1.Folders.Contains(childFolder))
                {
                    folder1.Folders.Add(childFolder);
                }

                directoryInfo = directoryInfo.Parent;

                childFolder = folder1;
            }

            if (!_rootFolder.Folders.Contains(childFolder))
            {
                _rootFolder.Folders.Add(childFolder);
                childFolder.Parent = _rootFolder;
            }
        }

        private IEnumerable<SlnFolder> EnumerateFolders(SlnFolder folder)
        {
            foreach (SlnFolder child in folder.Folders)
            {
                foreach (SlnFolder enumerateFolder in EnumerateFolders(child))
                {
                    yield return enumerateFolder;
                }
            }

            yield return folder;
        }
    }
}