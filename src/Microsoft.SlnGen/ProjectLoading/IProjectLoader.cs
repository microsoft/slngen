// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System.Collections.Generic;

namespace Microsoft.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents an interface for loading MSBuild projects.
    /// </summary>
    internal interface IProjectLoader
    {
        /// <summary>
        /// Loads the specified projects and their references.
        /// </summary>
        /// <param name="projectPaths">An <see cref="IEnumerable{String}"/> containing paths to the projects to load.</param>
        /// <param name="projectCollection">A <see cref="ProjectCollection" /> to load projects into.</param>
        /// <param name="globalProperties">A <see cref="IDictionary{String,String}" /> containing global properties to use when evaluation projects.</param>
        void LoadProjects(IEnumerable<string> projectPaths, ProjectCollection projectCollection, IDictionary<string, string> globalProperties);
    }
}