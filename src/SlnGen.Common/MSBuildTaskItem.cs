// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;

namespace SlnGen.Common
{
    public sealed class MSBuildTaskItem : IMSBuildItem
    {
        private readonly ITaskItem _taskItem;

        public MSBuildTaskItem(ITaskItem taskItem)
        {
            _taskItem = taskItem;
        }

        public string ItemSpec => _taskItem.ItemSpec;

        public string GetMetadata(string name) => _taskItem.GetMetadata(name);
    }
}