// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    public sealed class DevelopmentEnvironment
    {
        public DevelopmentEnvironment(params string[] errors)
            : this()
        {
            Errors = errors.ToList();
        }

        public DevelopmentEnvironment()
        {
        }

        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the current build environment is CoreXT.
        /// </summary>
        public bool IsCorext { get; set; }

        public FileInfo MSBuildDll { get; set; }

        public FileInfo MSBuildExe { get; set; }

        public bool Success => Errors?.Count == 0;

        public VisualStudioInstance VisualStudio { get; set; }
    }
}