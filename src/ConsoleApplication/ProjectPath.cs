using System;
using System.Collections.Generic;
using System.IO;

namespace SlnGen
{
    internal sealed class ProjectPath
    {
        internal static readonly ProjectPath EmptyPath = new ProjectPath(String.Empty, String.Empty, String.Empty);

        private const string DrivePrefix = "drive_";

        private const string NetworkPrefix = "network_";

        private static readonly char[] DirectoryChars = {Path.DirectorySeparatorChar};

        private ProjectPath(string theDirectory, string theProject, string theExtension)
        {
            DirectoryName = theDirectory;
            ProjectName = theProject;
            Extension = theExtension;
            FullPath = String.Empty;
            PathComponents = new List<string>();

            FullName = Path.Combine(theDirectory, theProject + theExtension);

            if (String.IsNullOrEmpty(DirectoryName))
            {
                return;
            }

            PathComponents.AddRange(DirectoryName.Split(DirectoryChars, StringSplitOptions.RemoveEmptyEntries));
            string volume = PathComponents[0];

            if (volume.EndsWith(Path.VolumeSeparatorChar.ToString(), StringComparison.Ordinal))
            {
                PathComponents[0] = DrivePrefix + volume.Substring(0, volume.Length - 1);
            }
            else
            {
                PathComponents[0] = NetworkPrefix + volume;
            }

            PathComponents = PathComponents;
            FullPath = Path.Combine(PathComponents.ToArray());
        }

        private ProjectPath(IEnumerable<string> theVsCompatibleComponents, int levelsDown)
        {
            DirectoryName = String.Empty;
            PathComponents = new List<string>(theVsCompatibleComponents);
            FullPath = Path.Combine(PathComponents.ToArray());

            if (levelsDown > 0)
            {
                PathComponents.RemoveRange(PathComponents.Count - levelsDown, levelsDown);
            }

            List<string> fileComponents = new List<string>(PathComponents);

            if (fileComponents[0].StartsWith(DrivePrefix, StringComparison.Ordinal))
            {
                fileComponents[0] = fileComponents[0].Substring(DrivePrefix.Length) + Path.VolumeSeparatorChar;

                if (fileComponents.Count == 1)
                {
                    fileComponents.Add(String.Empty);
                }
            }

            if (fileComponents[0].StartsWith(NetworkPrefix, StringComparison.Ordinal))
            {
                fileComponents[0] = fileComponents[0].Substring(NetworkPrefix.Length);

                if (fileComponents.Count == 1)
                {
                    fileComponents.Add(String.Empty);
                }

                fileComponents.Insert(0, String.Empty);
                fileComponents.Insert(0, String.Empty);
            }

            DirectoryName = Path.Combine(fileComponents.ToArray());
            FullName = DirectoryName;
        }

        public string DirectoryName { get; }

        public string Extension { get; }

        public string FullName { get; }

        public string FullPath { get; }
        public List<string> PathComponents { get; }
        public string ProjectName { get; }

        public string VsName
        {
            get
            {
                if (PathComponents.Count == 0)
                {
                    return String.Empty;
                }
                return PathComponents[PathComponents.Count - 1];
            }
        }

        public static ProjectPath FromFile(string theFilePath)
        {
            string fullPath = String.Empty;
            string filePath = String.Empty;
            string dirPath = String.Empty;
            string extensionPath = String.Empty;
            try
            {
                fullPath = Path.GetFullPath(theFilePath);
                filePath = Path.GetFileNameWithoutExtension(fullPath);
                dirPath = Path.GetDirectoryName(fullPath) ?? String.Empty;
                extensionPath = Path.GetExtension(fullPath);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                // Ignored
            }

            if (String.IsNullOrEmpty(fullPath))
            {
                return EmptyPath;
            }

            return new ProjectPath(dirPath, filePath, extensionPath);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProjectPath other) || GetType() != other.GetType())
            {
                return false;
            }

            return FullName.Equals(other.FullName, StringComparison.OrdinalIgnoreCase) &&
                   FullPath.Equals(other.FullPath, StringComparison.OrdinalIgnoreCase);
        }

        public ProjectPath GetAncestorDirectory(int levelsDown)
        {
            if (PathComponents.Count == 0 || levelsDown >= PathComponents.Count)
            {
                return EmptyPath;
            }

            if (levelsDown <= 0)
            {
                levelsDown = 0;
            }

            return new ProjectPath(PathComponents, levelsDown);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(FullName);
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}