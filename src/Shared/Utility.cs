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

        /// <summary>
        /// Gets a value indicating whether or not the current operating system is macOS.
        /// </summary>
        public static readonly bool RunningOnMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Attempts to find the specified executable on the PATH.
        /// </summary>
        /// <param name="environmentProvider">An <see cref="IEnvironmentProvider" /> to use when accessing the environment.</param>
        /// <param name="exe">The name of the executable to find.</param>
        /// <param name="validator">A <see cref="Func{T, TResult}" /> that validates if a found item on the PATH is what the caller is looking for.</param>
        /// <param name="fileInfo">Receives a <see cref="FileInfo" /> object with details about the executable if found.</param>
        /// <returns><code>true</code> if an executable could be found, otherwise <code>false</code>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="environmentProvider" /> is <c>null</c>.</exception>
        public static bool TryFindOnPath(IEnvironmentProvider environmentProvider, string exe, Func<FileInfo, bool> validator, out FileInfo fileInfo)
        {
            if (environmentProvider is null)
            {
                throw new ArgumentNullException(nameof(environmentProvider));
            }

            fileInfo = (environmentProvider.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => new DirectoryInfo(i.Trim()))
                .Where(i => i.Exists)
                .Select(i => new FileInfo(Path.Combine(i.FullName, $"{exe}").ToFullPathInCorrectCase()))
                .FirstOrDefault(i => i.Exists && (validator == null || validator(i)));

            return fileInfo != null;
        }

        /// <summary>
        /// Writes the specified error to the console.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter" /> to write the error to.</param>
        /// <param name="message">The message to write to <see cref="Console.Error" />.</param>
        /// <param name="args">An array of objects to write using <see cref="message" />.</param>
        public static void WriteError(TextWriter writer, string message, params object[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;

            Console.ForegroundColor = ConsoleColor.Red;

            writer.WriteLine(message, args);

            Console.ResetColor();
        }
    }
}