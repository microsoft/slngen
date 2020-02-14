// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal class MockTaskItem : Dictionary<string, string>, ITaskItem
    {
        public MockTaskItem()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public MockTaskItem(string itemSpec)
            : this()
        {
            ItemSpec = itemSpec;
        }

        public string ItemSpec { get; set; }

        public int MetadataCount => Count;

        public ICollection MetadataNames => Keys;

        public IDictionary CloneCustomMetadata()
        {
            return new Dictionary<string, string>(this);
        }

        public void CopyMetadataTo(ITaskItem destinationItem)
        {
            foreach (KeyValuePair<string, string> metadatum in this)
            {
                destinationItem.SetMetadata(metadatum.Key, metadatum.Value);
            }
        }

        public string GetMetadata(string metadataName) => this[metadataName];

        public void RemoveMetadata(string metadataName)
        {
            if (ContainsKey(metadataName))
            {
                Remove(metadataName);
            }
        }

        public void SetMetadata(string metadataName, string metadataValue) => this[metadataName] = metadataValue;
    }
}