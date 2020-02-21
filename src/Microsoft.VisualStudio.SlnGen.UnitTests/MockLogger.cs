// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal class MockLogger : ILogger
    {
        private readonly ConcurrentQueue<BuildEventArgs> _events = new ConcurrentQueue<BuildEventArgs>();

        public IReadOnlyCollection<BuildEventArgs> Events => _events;

        public string Parameters { get; set; }

        public LoggerVerbosity Verbosity { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += (sender, args) => { _events.Enqueue(args); };

            if (eventSource is IEventSource2 eventSource2)
            {
                eventSource2.TelemetryLogged += (sender, args) => { _events.Enqueue(args); };
            }
        }

        public void Shutdown()
        {
        }
    }
}