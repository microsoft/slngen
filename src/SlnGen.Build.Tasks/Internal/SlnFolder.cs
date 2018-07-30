// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.IO;

namespace SlnGen.Build.Tasks.Internal
{
    internal sealed class SlnFolder
    {
        public static readonly Guid FolderProjectTypeGuid = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");

        public SlnFolder(string path, Guid folderGuid)
        {
            Name = Path.GetFileName(path);
            FullPath = path;
            FolderGuid = folderGuid;
        }

        public string FullPath { get; }

        public Guid FolderGuid { get; }

        public string Name { get; }

        public Guid ProjectTypeGuid => FolderProjectTypeGuid;
    }
}