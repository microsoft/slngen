// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    internal class ConsoleForwardingLogger : EventArgsDispatcher, IEventSource, ILogger
    {
        private readonly IConsole _console;
        private readonly bool _noWarn;
        private readonly (bool HasValue, string Arguments) _parameter;
        private ConsoleLogger _consoleLogger;
        private IEventSource _eventSource;
        private int _hasLoggedErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleForwardingLogger"/> class.
        /// </summary>
        /// <param name="parameter">The parameters.</param>
        /// <param name="noWarn">A flag indicating to disable warnings.</param>
        /// <param name="console">An optional <see cref="IConsole" /> to use.</param>
        public ConsoleForwardingLogger((bool HasValue, string Arguments) parameter, bool noWarn, IConsole console = null)
        {
            _parameter = parameter;
            _noWarn = noWarn;
            _console = console;
        }

        /// <summary>
        /// Gets a value indicating whether or not any errors have been logged.
        /// </summary>
        public bool HasLoggedErrors => _hasLoggedErrors != 0;

        /// <inheritdoc />
        public string Parameters { get; set; }

        /// <inheritdoc />
        public LoggerVerbosity Verbosity { get; set; }

        /// <inheritdoc />
        public void Initialize(IEventSource eventSource)
        {
            if (_console != null)
            {
                _consoleLogger = new ConsoleLogger(Verbosity, message => _console.Write(message), color => { }, () => { })
                {
                    Parameters = _parameter.Arguments.IsNullOrWhiteSpace() ? "ForceNoAlign=true;Summary" : _parameter.Arguments,
                };
            }
            else
            {
                _consoleLogger = new ConsoleLogger(Verbosity)
                {
                    Parameters = _parameter.Arguments.IsNullOrWhiteSpace() ? $"Verbosity={Verbosity};ForceNoAlign=true;Summary" : _parameter.Arguments,
                };
            }

            _eventSource = eventSource;

            _eventSource.AnyEventRaised += OnAnyEventRaised;

            _consoleLogger.Initialize(this);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            _consoleLogger.Shutdown();

            _eventSource.AnyEventRaised -= OnAnyEventRaised;
        }

        private new void Dispatch(BuildEventArgs e)
        {
            if (_hasLoggedErrors == 0 && e is BuildErrorEventArgs)
            {
                Interlocked.Exchange(ref _hasLoggedErrors, 1);
            }

            if (e is ProjectStartedEventArgs || e is ProjectFinishedEventArgs)
            {
                return;
            }

            if (_noWarn && e is BuildWarningEventArgs)
            {
                return;
            }

            if (e.BuildEventContext == null)
            {
                e.BuildEventContext = BuildEventContext.Invalid;
            }

            base.Dispatch(e);
        }

        private void OnAnyEventRaised(object sender, BuildEventArgs e) => Dispatch(e);
    }
}