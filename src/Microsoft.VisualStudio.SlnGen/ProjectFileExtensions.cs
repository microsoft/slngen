﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class that contains MSBuild project file extensions.
    /// </summary>
    internal static class ProjectFileExtensions
    {
        /// <summary>
        /// Azure SDK projects (.ccproj).
        /// </summary>
        public const string AzureSdk = ".ccproj";

        /// <summary>
        /// Azure Service Fabric projects (.sfproj).
        /// </summary>
        public const string AzureServiceFabric = ".sfproj";

        /// <summary>
        /// Visual C++ projects (.vcxproj).
        /// </summary>
        public const string Cpp = ".vcxproj";

        /// <summary>
        /// C# projects (.csproj).
        /// </summary>
        public const string CSharp = ".csproj";

        /// <summary>
        /// F# projects (.fsproj).
        /// </summary>
        public const string FSharp = ".fsproj";

        /// <summary>
        /// Visual J# projects (.vjsproj).
        /// </summary>
        public const string JSharp = ".vjsproj";

        /// <summary>
        /// Legacy C++ projects (.vcproj).
        /// </summary>
        public const string LegacyCpp = ".vcproj";

        /// <summary>
        /// Native projects (.nativeProj).
        /// </summary>
        public const string Native = ".nativeProj";

        /// <summary>
        /// Node JS projects (.njsproj)
        /// </summary>
        public const string NodeJS = ".njsproj";

        /// <summary>
        /// NuProj projects (.nuproj).
        /// </summary>
        public const string NuProj = ".nuproj";

        /// <summary>
        /// Shared project items (.projitems)
        /// </summary>
        public const string ProjItems = ".projitems";

        /// <summary>
        /// Scope SDK projects (.scopeproj).
        /// </summary>
        public const string Scope = ".scopeproj";

        /// <summary>
        /// Shared project (.shproj)
        /// </summary>
        public const string Shproj = ".shproj";

        /// <summary>
        /// SQL Server database projects (.sqlproj).
        /// </summary>
        public const string SqlServerDb = ".sqlproj";

        /// <summary>
        /// Visual C++ Shared Project Items.
        /// </summary>
        public const string VcxItems = ".vcxitems";

        /// <summary>
        /// Visual Basic projects (.vbproj).
        /// </summary>
        public const string VisualBasic = ".vbproj";

        /// <summary>
        /// Windows Application Packaging projects (.wapproj).
        /// </summary>
        public const string Wap = ".wapproj";

        /// <summary>
        /// WiX projects (.wixproj).
        /// </summary>
        public const string Wix = ".wixproj";
    }
}