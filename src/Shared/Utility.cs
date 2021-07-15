// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class containing utility methods.
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Gets a value indicating whether or not the current operating system is Windows.
        /// </summary>
        public static readonly bool RunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static readonly Lazy<string[]> PathExtLazy = new (() =>
        {
            string pathExt = Environment.GetEnvironmentVariable("PATHEXT");

            return string.IsNullOrWhiteSpace(pathExt)
                ? new[] { string.Empty }
                : pathExt.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
        });

        /// <summary>
        /// Attempts to find the specified executable on the PATH.
        /// </summary>
        /// <param name="exe">The name of the executable to find.</param>
        /// <param name="fileInfo">Receives a <see cref="FileInfo" /> object with details about the executable if found.</param>
        /// <returns><code>true</code> if an executable could be found, otherwise <code>false</code>.</returns>
        public static bool TryFindOnPath(string exe, out FileInfo fileInfo)
        {
            fileInfo = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => new DirectoryInfo(i.Trim()))
                .Where(i => i.Exists)
                .SelectMany(i => PathExtLazy.Value.Select(extension => new FileInfo(Path.Combine(i.FullName, $"{exe}{extension}").ToFullPathInCorrectCase())))
                .FirstOrDefault(i => i.Exists);

            return fileInfo != null;
        }

        /// <summary>
        /// Writes the specified error to the console.
        /// </summary>
        /// <param name="message">The message to write to <see cref="Console.Error" />.</param>
        /// <param name="args">An array of objects to write using <see cref="message" />.</param>
        public static void WriteError(string message, params object[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;

            Console.ForegroundColor = ConsoleColor.Red;

            Console.Error.WriteLine(message, args);

            Console.ResetColor();
        }
    }
}