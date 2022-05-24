// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    /// <summary>
    /// Represents extension methods.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Gets the full path to the current <see cref="IVsProject" /> object.
        /// </summary>
        /// <param name="project">The <see cref="IVsProject" /> to get the full path for.</param>
        /// <returns>The full path to the current project if available, otherwise <see cref="string.Empty" />.</returns>
        public static string GetFullPath(this IVsProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out string fullPath)) ? fullPath : string.Empty;
        }

        /// <summary>
        /// Gets a list of <see cref="IVsProject" /> objects in the current solution.
        /// </summary>
        /// <param name="solution">The <see cref="IVsSolution" /> to get the projects for.</param>
        /// <param name="flags">Optional <see cref="__VSENUMPROJFLAGS" /> to use.  Defaults to <see cref="__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION" />.</param>
        /// <returns>An <see cref="IEnumerable{T}" /> of <see cref="IVsProject" /> objects representing the projects in the solution.</returns>
        public static IEnumerable<IVsProject> GetProjects(this IVsSolution solution, __VSENUMPROJFLAGS flags = __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
            {
                yield break;
            }

            Guid guid = Guid.Empty;

            if (ErrorHandler.Failed(solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies hierarchies)) || hierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];

            while (hierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                yield return hierarchy[0] as IVsProject;
            }
        }

        /// <summary>
        /// Gets the specified property from the current <see cref="IVsSolution" /> object.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="solution">The current <see cref="IVsSolution" /> object to get the property for.</param>
        /// <param name="propertyId">A <see cref="__VSPROPID" /> representing the property.</param>
        /// <returns>The property value if one was found, otherwise <c>default(T)</c>.</returns>
        public static T GetProperty<T>(this IVsSolution solution, __VSPROPID propertyId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ErrorHandler.Succeeded(solution.GetProperty((int)propertyId, out object objectValue)) && objectValue is T value ? value : default;
        }
    }
}