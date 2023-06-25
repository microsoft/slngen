// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;

namespace Microsoft.VisualStudio.SlnGen.Launcher
{
    /// <summary>
    /// Represents a class used to launch Visual Studio.
    /// </summary>
    internal interface IVisualStudioLauncher
    {
        /// <summary>
        /// Launches Visual Studio.
        /// </summary>
        /// <param name="arguments">The current <see cref="ProgramArguments" />.</param>
        /// <param name="visualStudioInstance">A <see cref="VisualStudioInstance" /> object representing which instance of Visual Studio to launch.</param>
        /// <param name="solutionFileFullPath">The full path to the solution file.</param>
        /// <returns>true if Visual Studio was launched, otherwise false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="environmentProvider" /> is <c>null</c>.</exception>
        bool TryLaunch(ProgramArguments arguments, VisualStudioInstance visualStudioInstance, string solutionFileFullPath);
    }
}