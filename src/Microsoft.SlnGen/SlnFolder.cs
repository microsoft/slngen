// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents a folder in a Visual Studio solution file.
    /// </summary>
    public sealed class SlnFolder
    {
        public static readonly string FolderProjectTypeGuid = new Guid(VisualStudioProjectTypeGuids.SolutionFolder).ToSolutionString();

        public SlnFolder(string path)
        {
            Name = Path.GetFileName(path);
            FullPath = path;
            FolderGuid = Guid.NewGuid();
        }

        public Guid FolderGuid { get; }

        public List<SlnFolder> Folders { get; } = new List<SlnFolder>();

        public string FullPath { get; }

        public string Name { get; set; }

        public SlnFolder Parent { get; set; }

        public List<SlnProject> Projects { get; } = new List<SlnProject>();

        public string ProjectTypeGuid => FolderProjectTypeGuid;
    }
}