// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    internal static class ExtensionMethods
    {
        public static string GetFullPath(this IVsProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int result = project.GetMkDocument(VSConstants.VSITEMID_ROOT, out string fullPath);

            if (ErrorHandler.Succeeded(result))
            {
                return fullPath;
            }

            return string.Empty;
        }

        public static IEnumerable<IVsProject> GetProjects(this IVsSolution solution, __VSENUMPROJFLAGS flags = __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (solution == null)
            {
                yield break;
            }

            Guid guid = Guid.Empty;

            solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies hierarchies);

            if (hierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];

            while (hierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                yield return hierarchy[0] as IVsProject;
            }
        }
    }
}