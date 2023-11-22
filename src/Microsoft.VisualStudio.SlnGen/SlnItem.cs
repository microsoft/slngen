// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a set of solution items in a Visual Studio solution file.
    /// </summary>
    internal class SlnItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SlnItem"/> class.
        /// </summary>
        /// <param name="parentFolderGuid">The <see cref="Guid" /> of the parent folder.</param>
        /// <param name="folderGuid">The <see cref="Guid" /> of the folder.</param>
        /// <param name="solutionItems">The solution items in the folder</param>
        public SlnItem(Guid? parentFolderGuid, Guid folderGuid, IEnumerable<string> solutionItems)
        {
            this.ParentFolderGuid = parentFolderGuid;
            this.FolderGuid = folderGuid;
            this.SolutionItems = solutionItems.ToList();
        }

        /// <summary>
        /// Gets or sets the <see cref="Guid" /> of the parent folder.
        /// </summary>
        public Guid? ParentFolderGuid { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Guid" /> of the folder.
        /// </summary>
        public Guid FolderGuid { get; set; }

        /// <summary>
        /// Gets a <see cref="List{String}" /> of solution items in the folder.
        /// </summary>
        public List<string> SolutionItems { get; } = new ();

        /// <summary>
        /// Gets the project type GUID of the folder.
        /// </summary>
        public string ProjectTypeGuidString => new Guid(VisualStudioProjectTypeGuids.SolutionFolder).ToSolutionString();
    }
}
