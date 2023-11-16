// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class that forwards logging events to a console.
    /// </summary>
    internal class ConsoleForwardingLogger : EventArgsDispatcher, ILogger
    {
        private readonly IConsole _console;
        private ConsoleLogger _consoleLogger;
        private IEventSource _eventSource;
        private int _hasLoggedErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleForwardingLogger"/> class.
        /// </summary>
        /// <param name="console">The <see cref="IConsole" /> to forward events to.</param>
        public ConsoleForwardingLogger(IConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        /// <summary>
        /// Gets a value indicating whether or not any errors have been logged.
        /// </summary>
        public bool HasLoggedErrors => _hasLoggedErrors != 0;

        /// <summary>
        /// Gets or sets a value indicating whether or not warnings should be suppressed.
        /// </summary>
        public bool NoWarn { get; set; }

        /// <inheritdoc />
        public string Parameters { get; set; }

        /// <inheritdoc />
        public LoggerVerbosity Verbosity { get; set; }

        /// <inheritdoc />
        public void Initialize(IEventSource eventSource)
        {
            _consoleLogger = new ConsoleLogger(
                Verbosity,
                message => _console.Write(message),
                color => { _console.ForegroundColor = color; },
                () => { _console.ResetColor(); })
            {
                Parameters = Parameters,
            };

            _eventSource = eventSource;

            _eventSource.AnyEventRaised += OnAnyEventRaised;

            _consoleLogger.Initialize(this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            _eventSource.AnyEventRaised -= OnAnyEventRaised;

            _consoleLogger.Shutdown();
        }

        private new void Dispatch(BuildEventArgs e)
        {
            if (_hasLoggedErrors == 0 && e is BuildErrorEventArgs)
                Interlocked.Exchange(ref _hasLoggedErrors, 1);

            if (e is ProjectStartedEventArgs || e is ProjectFinishedEventArgs)
                // Don't send these events to the ConsoleLogger because its not useful when running SlnGen like they are when running MSBuild
                return;

            if (NoWarn && e is BuildWarningEventArgs)
                return;

            if (e.BuildEventContext == null)
                e.BuildEventContext = BuildEventContext.Invalid;

            base.Dispatch(e);
        }

        private void OnAnyEventRaised(object sender, BuildEventArgs e) => Dispatch(e);
    }
}