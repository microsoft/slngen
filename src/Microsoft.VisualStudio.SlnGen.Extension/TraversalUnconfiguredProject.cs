// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    [Export]
    [AppliesTo(UniqueCapability)]
    [ProjectTypeRegistration(
        projectTypeGuid: SlnGenPackage.ProjectTypeGuid,
        displayName: Language,
        displayProjectFileExtensions: "#2",
        defaultProjectExtension: SlnGenPackage.ProjectExtension,
        language: Language,
        resourcePackageGuid: SlnGenPackage.PackageGuidString,
        PossibleProjectExtensions = SlnGenPackage.ProjectExtension)]
    internal class TraversalUnconfiguredProject
    {
        internal const string Language = "Traversal";

        internal const string UniqueCapability = Language;

        [ImportingConstructor]
        public TraversalUnconfiguredProject(UnconfiguredProject unconfiguredProject)
        {
            ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        [Import]
        internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; private set; }

        [ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
        internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; private set; }

        internal IVsHierarchy ProjectHierarchy => ProjectHierarchies.Single().Value;

        [Import]
        internal IProjectThreadingService ProjectThreadingService { get; private set; }

        [Import]
        internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; private set; }

        [Import]
        internal ActiveConfiguredProject<TraversalConfiguredProject> TraversalActiveConfiguredProject { get; private set; }

        [Import]
        internal UnconfiguredProject UnconfiguredProject { get; private set; }
    }
}