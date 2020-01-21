// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace SlnGen.Common
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Retrieves the final path for the specified file.
        /// </summary>
        /// <param name="hFile">A handle to a file or directory.</param>
        /// <param name="lpszFilePath">A pointer to a buffer that receives the path of <paramref name="hFile" />.</param>
        /// <param name="cchFilePath">The size of <paramref name="lpszFilePath" />, in TCHARs. This value does not include a NULL termination character.</param>
        /// <param name="dwFlags">The type of result to return.</param>
        /// <returns>If the function succeeds, the return value is the length of the string received by <paramref name="lpszFilePath" />, in TCHARs. This value does not include the size of the terminating null character.</returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetFinalPathNameByHandle(SafeHandle hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, int cchFilePath, int dwFlags);
    }
}