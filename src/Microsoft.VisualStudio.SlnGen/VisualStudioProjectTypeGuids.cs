// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class that contains Visual Studio project type GUIDs.
    /// </summary>
    public static class VisualStudioProjectTypeGuids
    {
        /// <summary>
        /// Azure SDK project (.ccproj).
        /// </summary>
        public const string AzureSdk = "151D2E53-A2C4-4D7D-83FE-D05416EBD58E";

        /// <summary>
        /// Azure Service Fabric projects (.sfproj).
        /// </summary>
        public const string AzureServiceFabric = "A07B5EB6-E848-4116-A8D0-A826331D98C6";

        /// <summary>
        /// Visual C++ projects (.vcxproj).
        /// </summary>
        public const string Cpp = "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942";

        /// <summary>
        /// F# projects (.fsproj).
        /// </summary>
        public const string FSharp = "F2A71F9B-5D33-465A-A702-920D77279786";

        /// <summary>
        /// Visual J# projects (.vjsproj).
        /// </summary>
        public const string JSharp = "E6FDF86B-F3D1-11D4-8576-0002A516ECE8";

        /// <summary>
        /// Legacy C# projects (.csproj).
        /// </summary>
        public const string LegacyCSharpProject = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

        /// <summary>
        /// Legacy Visual Basic projects (.vbproj).
        /// </summary>
        public const string LegacyVisualBasicProject = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

        /// <summary>
        /// Microsoft.NET.Sdk C# projects (.csproj).
        /// </summary>
        public const string NetSdkCSharpProject = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";

        /// <summary>
        /// Microsoft.NET.Sdk Visual Basic projects (.vbproj).
        /// </summary>
        public const string NetSdkVisualBasicProject = "778DAE3C-4631-46EA-AA77-85C1314464D9";

        /// <summary>
        /// NuProj projects (.nuproj).
        /// </summary>
        public const string NuProj = "FF286327-C783-4F7A-AB73-9BCBAD0D4460";

        /// <summary>
        /// Scope SDK projects (.scopeproj).
        /// </summary>
        public const string ScopeProject = "202899A3-C531-4771-9089-0213D66978AE";

        /// <summary>
        /// Visual Studio solution folder.
        /// </summary>
        public const string SolutionFolder = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

        /// <summary>
        /// WiX projects (.wixproj).
        /// </summary>
        public const string Wix = "930C7802-8A8C-48F9-8165-68863BCCD9DD";
    }
}