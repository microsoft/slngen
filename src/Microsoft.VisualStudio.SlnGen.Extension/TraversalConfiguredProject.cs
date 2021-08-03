// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.ProjectSystem;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    [Export]
    [AppliesTo(TraversalUnconfiguredProject.UniqueCapability)]
    internal class TraversalConfiguredProject
    {
        internal ConfiguredProject ConfiguredProject { get; private set; }

        internal ProjectProperties Properties { get; private set; }
    }
}