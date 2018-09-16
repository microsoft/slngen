// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;

namespace SlnGen.Build.Tasks.Internal
{
    internal sealed class SlnItem
    {
        public SlnItem(string path, string targetFolder)
        {
            if (String.IsNullOrWhiteSpace(targetFolder))
            {
                targetFolder = "Solution Items";
            }

            FullPath = path;
            TargetFolder = targetFolder;
        }

        public string FullPath { get; }

        public string TargetFolder { get; }
    }
}