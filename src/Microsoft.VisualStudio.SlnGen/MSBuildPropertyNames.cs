// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class that contains MSBuild property names.
    /// </summary>
    public static class MSBuildPropertyNames
    {
        /// <summary>
        /// Represents the BuildingProject property.
        /// </summary>
        public const string BuildingProject = nameof(BuildingProject);

        /// <summary>
        /// Represents the DesignTimeBuild property.
        /// </summary>
        public const string DesignTimeBuild = nameof(DesignTimeBuild);

        /// <summary>
        /// Represents the ExcludeRestorePackageImports property.
        /// </summary>
        public const string ExcludeRestorePackageImports = nameof(ExcludeRestorePackageImports);

        /// <summary>
        /// Represents the IncludeInSolutionFile property.
        /// </summary>
        public const string IncludeInSolutionFile = nameof(IncludeInSolutionFile);

        /// <summary>
        /// Represents the IsSlnGen property.
        /// </summary>
        public const string IsSlnGen = nameof(IsSlnGen);

        /// <summary>
        /// Represents the IsTraversal property.
        /// </summary>
        public const string IsTraversal = nameof(IsTraversal);

        /// <summary>
        /// Represents the IsTraversalProject property.
        /// </summary>
        public const string IsTraversalProject = nameof(IsTraversalProject);

        /// <summary>
        /// Represents the ProjectGuid property.
        /// </summary>
        public const string ProjectGuid = nameof(ProjectGuid);

        /// <summary>
        /// Represents the ProjectTypeGuid property.
        /// </summary>
        public const string ProjectTypeGuid = nameof(ProjectTypeGuid);

        /// <summary>
        /// Represents the SlnGenBinLog property.
        /// </summary>
        public const string SlnGenBinLog = nameof(SlnGenBinLog);

        /// <summary>
        /// Represents the SlnGenDebug property.
        /// </summary>
        public const string SlnGenDebug = nameof(SlnGenDebug);

        /// <summary>
        /// Represents the SlnGenDevEnvFullPath property.
        /// </summary>
        public const string SlnGenDevEnvFullPath = nameof(SlnGenDevEnvFullPath);

        /// <summary>
        /// Represents the SlnGenFolders property.
        /// </summary>
        public const string SlnGenFolders = nameof(SlnGenFolders);

        /// <summary>
        /// Represents the SlnGenGlobalProperties property.
        /// </summary>
        public const string SlnGenGlobalProperties = nameof(SlnGenGlobalProperties);

        /// <summary>
        /// Represents the SlnGenIsDeployable property.
        /// </summary>
        public const string SlnGenIsDeployable = nameof(SlnGenIsDeployable);

        /// <summary>
        /// Represents the SlnGenLaunchVisualStudio property.
        /// </summary>
        public const string SlnGenLaunchVisualStudio = nameof(SlnGenLaunchVisualStudio);

        /// <summary>
        /// Visual Studio version to add to the generated solution file.
        /// Specify "default" to use the same version selection logic launching does.
        /// </summary>
        public const string SlnGenVSVersion = nameof(SlnGenVSVersion);

        /// <summary>
        /// Represents the SlnGenLoadProjects property.
        /// </summary>
        public const string SlnGenLoadProjects = nameof(SlnGenLoadProjects);

        /// <summary>
        /// Represents the SlnGenProjectName property.
        /// </summary>
        public const string SlnGenProjectName = nameof(SlnGenProjectName);

        /// <summary>
        /// Represents the SlnGenSolutionFileFullPath property.
        /// </summary>
        public const string SlnGenSolutionFileFullPath = nameof(SlnGenSolutionFileFullPath);

        /// <summary>
        /// Represents the name of a solution folder to place the project in.
        /// </summary>
        public const string SlnGenSolutionFolder = nameof(SlnGenSolutionFolder);

        /// <summary>
        /// Represents the UsingMicrosoftNETSdk property.
        /// </summary>
        public const string UsingMicrosoftNETSdk = nameof(UsingMicrosoftNETSdk);

        /// <summary>
        /// Represents the SlnGenIsBuildable property.
        /// </summary>
        public const string SlnGenIsBuildable = nameof(SlnGenIsBuildable);
    }
}