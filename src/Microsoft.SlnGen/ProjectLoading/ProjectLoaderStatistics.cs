// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.SlnGen.ProjectLoading
{
    /// <summary>
    /// Represents statistics of operations performed by the <see cref="LegacyProjectLoader"/> class.
    /// </summary>
    public sealed class ProjectLoaderStatistics
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _projectLoadTimes = new ConcurrentDictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the <see cref="IEnumerable{T}" /> containing the project load times.
        /// </summary>
        public IEnumerable<KeyValuePair<string, TimeSpan>> ProjectLoadTimes => _projectLoadTimes;

        /// <summary>
        /// Attempts to add the load time for the specified project.
        /// </summary>
        /// <param name="path">The full path to the project.</param>
        /// <param name="timeSpan">The amount of time it took for the project to load.</param>
        /// <returns>true if the project was successfully added, otherwise false.</returns>
        internal bool TryAddProjectLoadTime(string path, TimeSpan timeSpan)
        {
            return _projectLoadTimes.TryAdd(path, timeSpan);
        }
    }
}