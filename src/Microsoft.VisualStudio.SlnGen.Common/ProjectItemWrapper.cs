// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Acts as an <see cref="ITaskItem" /> wrapper for a <see cref="ProjectItem" /> object.
    /// </summary>
    internal sealed class ProjectItemWrapper : ITaskItem
    {
        private readonly ProjectItemInstance _item;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectItemWrapper"/> class.
        /// </summary>
        /// <param name="item">The <see cref="ProjectItem" /> object to wrap.</param>
        public ProjectItemWrapper(ProjectItemInstance item)
        {
            _item = item;
            ItemSpec = item.EvaluatedInclude;
            MetadataCount = item.MetadataCount;
            MetadataNames = item.Metadata.Select(i => i.Name).ToList();
        }

        /// <inheritdoc />
        public string ItemSpec { get; set; }

        /// <inheritdoc />
        public int MetadataCount { get; }

        /// <inheritdoc />
        public ICollection MetadataNames { get; }

        /// <inheritdoc />
        public IDictionary CloneCustomMetadata()
        {
            return _item.Metadata.ToDictionary(i => i.Name, i => i.EvaluatedValue);
        }

        /// <inheritdoc />
        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetMetadata(string metadataName)
        {
            return _item.GetMetadataValue(metadataName);
        }

        /// <inheritdoc />
        public void RemoveMetadata(string metadataName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetMetadata(string metadataName, string metadataValue)
        {
            throw new NotImplementedException();
        }
    }
}