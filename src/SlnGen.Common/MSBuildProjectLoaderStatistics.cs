// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SlnGen.Common
{
    /// <summary>
    /// Represents statistics of operations performed by the <see cref="MSBuildProjectLoader"/> class.
    /// </summary>
    public sealed class MSBuildProjectLoaderStatistics
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _projectLoadTimes = new ConcurrentDictionary<string, TimeSpan>();

        public IEnumerable<KeyValuePair<string, TimeSpan>> ProjectLoadTimes => _projectLoadTimes;

        internal bool TryAddProjectLoadTime(string path, TimeSpan timeSpan)
        {
            return _projectLoadTimes.TryAdd(path, timeSpan);
        }
    }
}