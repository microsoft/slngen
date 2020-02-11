// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents a class that contains MSBuild property names.
    /// </summary>
    public static class MSBuildPropertyNames
    {
        public const string AssemblyName = nameof(AssemblyName);

        public const string BuildingProject = nameof(BuildingProject);

        public const string DesignTimeBuild = nameof(DesignTimeBuild);

        public const string ExcludeRestorePackageImports = nameof(ExcludeRestorePackageImports);

        public const string IncludeInSolutionFile = nameof(IncludeInSolutionFile);

        public const string IsTraversal = nameof(IsTraversal);

        public const string IsTraversalProject = nameof(IsTraversalProject);

        public const string ProjectGuid = nameof(ProjectGuid);

        public const string ProjectTypeGuid = nameof(ProjectTypeGuid);

        public const string SlnGenDevEnvFullPath = nameof(SlnGenDevEnvFullPath);

        public const string SlnGenFolders = nameof(SlnGenFolders);

        public const string SlnGenIsDeployable = nameof(SlnGenIsDeployable);

        public const string SlnGenLaunchVisualStudio = nameof(SlnGenLaunchVisualStudio);

        public const string SlnGenLoadProjects = nameof(SlnGenLoadProjects);

        public const string SlnGenSolutionFileFullPath = nameof(SlnGenSolutionFileFullPath);

        public const string SlnGenUseShellExecute = nameof(SlnGenUseShellExecute);

        public const string UsingMicrosoftNETSdk = nameof(UsingMicrosoftNETSdk);
    }
}