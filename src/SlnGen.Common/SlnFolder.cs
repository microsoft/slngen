// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace SlnGen.Common
{
    public sealed class SlnFolder
    {
        public static readonly Guid FolderProjectTypeGuid = new Guid("{2150E333-8FDC-42A3-9474-1A3956D46DE8}");

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

        public Guid ProjectTypeGuid => FolderProjectTypeGuid;
    }
}