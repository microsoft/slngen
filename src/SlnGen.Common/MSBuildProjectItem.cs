// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;

namespace SlnGen.Common
{
    public sealed class MSBuildProjectItem : IMSBuildItem
    {
        private readonly ProjectItem _projectItem;

        public MSBuildProjectItem(ProjectItem projectItem)
        {
            _projectItem = projectItem;
        }

        public string ItemSpec => _projectItem.EvaluatedInclude;

        public string GetMetadata(string name) => _projectItem.GetMetadataValue(name);
    }
}