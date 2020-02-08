// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System.Collections.Generic;
using System.Diagnostics;

namespace SlnGen.Common
{
    /// <summary>
    /// Represents an interface for loading MSBuild projects.
    /// </summary>
    public interface IMSBuildProjectLoader
    {
        /// <summary>
        /// Loads the specified projects and their references.
        /// </summary>
        /// <param name="projectCollection">A <see cref="ProjectCollection" /> to load projects into.</param>
        /// <param name="globalProperties">A <see cref="IDictionary{String,String}" /> containing global properties to use when evaluation projects.</param>
        /// <param name="projectPaths">An <see cref="IEnumerable{String}"/> containing paths to the projects to load.</param>
        void LoadProjects(ProjectCollection projectCollection, IDictionary<string, string> globalProperties, IEnumerable<string> projectPaths);
    }

    public static class MSBuildProjectLoaderFactory
    {
        public static IMSBuildProjectLoader Create(string msbuildExePath, ISlnGenLogger logger)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(msbuildExePath);

            if (fileVersionInfo.FileMajorPart >= 16 && fileVersionInfo.FileMinorPart >= 4)
            {
                return new ProjectGraphProjectLoader(logger, msbuildExePath);
            }

            return new MSBuildProjectLoader(logger);
        }
    }
}