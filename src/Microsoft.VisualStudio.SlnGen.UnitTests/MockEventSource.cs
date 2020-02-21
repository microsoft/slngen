// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal class MockEventSource : EventArgsDispatcher
    {
        public MockEventSource(params ILogger[] loggers)
            : this(loggers.ToList())
        {
        }

        public MockEventSource(IEnumerable<ILogger> loggers)
        {
            foreach (ILogger logger in loggers)
            {
                logger.Initialize(this);
            }
        }

        public void DispatchError(string message) => Dispatch(new BuildErrorEventArgs(null, null, null, 0, 0, 0, 0, message, null, null));

        public void DispatchMessage(string message, MessageImportance importance = MessageImportance.Normal) => Dispatch(new BuildMessageEventArgs(message, null, null, importance));

        public void DispatchWarning(string message) => Dispatch(new BuildWarningEventArgs(null, null, null, 0, 0, 0, 0, message, null, null));
    }
}