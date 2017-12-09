using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SlnGen.Build.Tasks.Internal
{
    /// <summary>
    /// Represents statistics of operations performed by the <see cref="MSBuildProjectLoader"/> class.
    /// </summary>
    internal sealed class MSBuildProjectLoaderStatistics
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _projectLoadTimes = new ConcurrentDictionary<string, TimeSpan>();

        public IEnumerable<KeyValuePair<string, TimeSpan>> ProjectLoadTimes => _projectLoadTimes;

        internal bool TryAddProjectLoadTime(string path, TimeSpan timeSpan)
        {
            return _projectLoadTimes.TryAdd(path, timeSpan);
        }
    }
}