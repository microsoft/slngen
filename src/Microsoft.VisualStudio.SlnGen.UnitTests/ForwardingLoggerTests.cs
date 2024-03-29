﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.
#if NETFRAMEWORK
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ForwardingLoggerTests : TestBase
    {
        [Fact]
        public void NoWarn()
        {
            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider();

            MockLogger logger = new MockLogger();

            ForwardingLogger forwardingLogger = new ForwardingLogger(environmentProvider, new[] { logger }, noWarn: true);

            MockEventSource eventSource = new MockEventSource(forwardingLogger);

            eventSource.DispatchError("Error");
            eventSource.DispatchWarning("Warning");
            eventSource.DispatchMessage("Message");

            logger.Events.Select(i => i.Message).ShouldBe(new[] { "Build Started", "Error", "Message" });
        }
    }
}
#endif