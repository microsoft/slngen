// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a folder in a Visual Studio solution file.
    /// </summary>
    public sealed class SlnFolder
    {
        /// <summary>
        /// The project type GUID for a folder.
        /// </summary>
        public static readonly string FolderProjectTypeGuid = new Guid(VisualStudioProjectTypeGuids.SolutionFolder).ToSolutionString();

        /// <summary>
        /// Initializes a new instance of the <see cref="SlnFolder"/> class.
        /// </summary>
        /// <param name="path">The full path of the folder.</param>
        public SlnFolder(string path)
        {
            Name = Path.GetFileName(path);
            FullPath = path;
            FolderGuid = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the <see cref="Guid" /> of the folder.
        /// </summary>
        public Guid FolderGuid { get; }

        /// <summary>
        /// Gets a <see cref="List{SlnFolder}" /> of child folders.
        /// </summary>
        public List<SlnFolder> Folders { get; } = new ();

        /// <summary>
        /// Gets the full path of the folder.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets or sets the name of the folder.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parent folder.
        /// </summary>
        public SlnFolder Parent { get; set; }

        /// <summary>
        /// Gets a <see cref="List{SlnProject}" /> of projects in the folder.
        /// </summary>
        public List<SlnProject> Projects { get; } = new ();

        /// <summary>
        /// Gets the project type GUID of the folder.
        /// </summary>
        public string ProjectTypeGuid => FolderProjectTypeGuid;
    }
}