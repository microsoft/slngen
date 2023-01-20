// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using System;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal static class CustomProjectCreatorTemplates
    {
        public static ProjectCreator SharedProject(this ProjectCreatorTemplates templates, string path, out ProjectCreator sharedProjectItems)
        {
            string sharedProjectGuid = Guid.NewGuid().ToString("D").ToLowerInvariant();

            string name = Path.GetFileNameWithoutExtension(path);

            sharedProjectItems = ProjectCreator.Create(
                    path: Path.ChangeExtension(path, ".projitems"),
                    projectFileOptions: NewProjectFileOptions.IncludeAllOptions)
                .PropertyGroup()
                    .Property("MSBuildAllProjects", "$(MSBuildAllProjects);$(MSBuildThisFileFullPath)", condition: "'$(MSBuildVersion)' == '' Or '$(MSBuildVersion)' < '16.0'")
                    .Property("HasSharedItems", "true")
                    .Property("SharedGUID", sharedProjectGuid)
                .PropertyGroup(label: "Configuration")
                    .Property("Import_RootNamespace", name);

            return ProjectCreator.Create(
                    path: path,
                    projectFileOptions: NewProjectFileOptions.IncludeAllOptions)
                .PropertyGroup(label: "Globals")
                    .Property("ProjectGuid", sharedProjectGuid)
                    .Property("MinimumVisualStudioVersion", "14.0")
                .Import(@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props", condition: @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')")
                .Import(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.Default.props")
                .Import(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.Common.props")
                .PropertyGroup()
                .Import($"{name}.projitems", label: "Shared")
                .Import(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\CodeSharing\Microsoft.CodeSharing.CSharp.targets");
        }
    }
}