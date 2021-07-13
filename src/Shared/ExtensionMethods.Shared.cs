// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a set of extension methods.
    /// </summary>
    internal static partial class ExtensionMethods
    {
        /// <inheritdoc cref="string.IsNullOrWhiteSpace" />
        [DebuggerStepThrough]
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Returns the absolute path for the specified path string in the correct case according to the file system.
        /// </summary>
        /// <param name="path">The string.</param>
        /// <returns>Full path in correct case.</returns>
        public static string ToFullPathInCorrectCase(this string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return path;
            }

            if (Utility.RunningOnWindows)
            {
                using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                StringBuilder stringBuilder = new StringBuilder(GetFinalPathNameByHandle(stream.SafeFileHandle, null, 0, 0));

                GetFinalPathNameByHandle(stream.SafeFileHandle, stringBuilder, stringBuilder.Capacity, 0);

                return stringBuilder.ToString(4, stringBuilder.Capacity - 5);
            }

            return path;
        }

        /// <summary>
        /// Retrieves the final path for the specified file.
        /// </summary>
        /// <param name="hFile">A handle to a file or directory.</param>
        /// <param name="lpszFilePath">A pointer to a buffer that receives the path of <paramref name="hFile" />.</param>
        /// <param name="cchFilePath">The size of <paramref name="lpszFilePath" />, in TCHARs. This value does not include a NULL termination character.</param>
        /// <param name="dwFlags">The type of result to return.</param>
        /// <returns>If the function succeeds, the return value is the length of the string received by <paramref name="lpszFilePath" />, in TCHARs. This value does not include the size of the terminating null character.</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetFinalPathNameByHandle(SafeHandle hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath, int cchFilePath, int dwFlags);
    }
}