// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Diagnostics;

namespace Microsoft.VisualStudio.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents a factory class for creating an instance of a class that implements <see cref="IProjectLoader" />.
    /// </summary>
    internal static class ProjectLoaderFactory
    {
        /// <summary>
        /// Creates an appropriate instance of a class that implements <see cref="IProjectLoader" />.
        /// </summary>
        /// <param name="msbuildExePath">The full path to MSBuild.exe.</param>
        /// <param name="logger">An <see cref="ISlnGenLogger" /> object to use for logging.</param>
        /// <returns>An <see cref="IProjectLoader" /> object that can be used to load MSBuild projects.</returns>
        public static IProjectLoader Create(string msbuildExePath, ISlnGenLogger logger)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(msbuildExePath);

            // MSBuild 16.4 and above use the Static Graph API
            if (fileVersionInfo.FileMajorPart >= 16 && fileVersionInfo.FileMinorPart >= 4)
            {
                return new ProjectGraphProjectLoader(msbuildExePath);
            }

            return new LegacyProjectLoader(logger);
        }
    }
}