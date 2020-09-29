﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.CommandLine;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a logger that forwards events to other loggers.
    /// </summary>
    public class ForwardingLogger : EventArgsDispatcher, IEventSource2, ILogger, ISlnGenLogger
    {
        private static readonly Lazy<ProcessBinaryLoggerDelegate> ProcessBinaryLoggerDelegateLazy = new Lazy<ProcessBinaryLoggerDelegate>(
            () => Delegate.CreateDelegate(
                typeof(ProcessBinaryLoggerDelegate),
                typeof(MSBuildApp).GetMethod("ProcessBinaryLogger", BindingFlags.Static | BindingFlags.NonPublic) !) as ProcessBinaryLoggerDelegate);

        private static readonly Lazy<ProcessLoggerSwitchDelegate> ProcessLoggerSwitchDelegateLazy = new Lazy<ProcessLoggerSwitchDelegate>(
            () => Delegate.CreateDelegate(
                typeof(ProcessLoggerSwitchDelegate),
                typeof(MSBuildApp).GetMethod("ProcessLoggerSwitch", BindingFlags.Static | BindingFlags.NonPublic) !) as ProcessLoggerSwitchDelegate);

        private static readonly Lazy<ProcessVerbositySwitchDelegate> ProcessVerbositySwitchDelegateLazy = new Lazy<ProcessVerbositySwitchDelegate>(
            () => Delegate.CreateDelegate(
                typeof(ProcessVerbositySwitchDelegate),
                typeof(MSBuildApp).GetMethod("ProcessVerbositySwitch", BindingFlags.Static | BindingFlags.NonPublic) !) as ProcessVerbositySwitchDelegate);

        private readonly bool _noWarn;

        private IEventSource _eventSource;
        private int _hasLoggedErrors;
        private int _projectId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardingLogger"/> class.
        /// </summary>
        /// <param name="loggers">A <see cref="IEnumerable{ILogger}" /> to forward logging events to.</param>
        /// <param name="noWarn">Indicates whether or not all warnings should be suppressed.</param>
        public ForwardingLogger(IEnumerable<ILogger> loggers, bool noWarn)
        {
            if (loggers == null)
            {
                throw new ArgumentNullException(nameof(loggers));
            }

            _noWarn = noWarn;

            Loggers = loggers.ToList();

            IsDiagnostic = Loggers.Any(i => i.Verbosity == LoggerVerbosity.Diagnostic);
        }

        private delegate void ProcessBinaryLoggerDelegate(string[] parameters, ArrayList loggers, ref LoggerVerbosity verbosity);

        private delegate ArrayList ProcessLoggerSwitchDelegate(string[] parameters, LoggerVerbosity verbosity);

        private delegate LoggerVerbosity ProcessVerbositySwitchDelegate(string value);

        /// <inheritdoc />
        public event TelemetryEventHandler TelemetryLogged;

        /// <inheritdoc />
        public bool HasLoggedErrors => _hasLoggedErrors != 0;

        /// <inheritdoc />
        public bool IsDiagnostic { get; }

        /// <summary>
        /// Gets the loggers.
        /// </summary>
        public IReadOnlyCollection<ILogger> Loggers { get; }

        /// <inheritdoc />
        public int NextProjectId => Interlocked.Increment(ref _projectId);

        /// <inheritdoc />
        public string Parameters { get; set; }

        /// <inheritdoc />
        public LoggerVerbosity Verbosity { get; set; }

        /// <summary>
        /// Parses binary logger parameters.
        /// </summary>
        /// <param name="parameters">The parameters to parse.</param>
        /// <returns>An <see cref="IEnumerable{ILogger}" /> containing the loggers.</returns>
        public static IEnumerable<ILogger> ParseBinaryLoggerParameters(string parameters)
        {
            ArrayList loggers = new ArrayList();

            LoggerVerbosity loggerVerbosity = LoggerVerbosity.Normal;
            try
            {
                ProcessBinaryLoggerDelegateLazy.Value(new[] { parameters }, loggers, ref loggerVerbosity);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);

                yield break;
            }

            foreach (object logger in loggers)
            {
                yield return logger as ILogger;
            }
        }

        /// <summary>
        /// Parses logger parameters.
        /// </summary>
        /// <param name="parameters">The logger parameters to parse.</param>
        /// <returns>An <see cref="IEnumerable{ILogger}" /> containing the loggers.</returns>
        public static IEnumerable<ILogger> ParseLoggerParameters(string[] parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                yield break;
            }

            ArrayList loggers;
            try
            {
                loggers = ProcessLoggerSwitchDelegateLazy.Value(parameters, LoggerVerbosity.Normal);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);

                yield break;
            }

            foreach (object logger in loggers)
            {
                yield return logger as ILogger;
            }
        }

        /// <summary>
        /// Parses the verbosity parameters.
        /// </summary>
        /// <param name="parameters">The user supplied parameters.</param>
        /// <returns>The <see cref="LoggerVerbosity" /> if the parameters were successfully parsed, otherwise null.</returns>
        public static LoggerVerbosity ParseLoggerVerbosity(string parameters)
        {
            if (parameters.IsNullOrWhiteSpace())
            {
                return LoggerVerbosity.Normal;
            }

            try
            {
                return ProcessVerbositySwitchDelegateLazy.Value(parameters);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);

                return LoggerVerbosity.Normal;
            }
        }

        /// <inheritdoc />
        public void Initialize(IEventSource eventSource)
        {
            _eventSource = eventSource;

            _eventSource.AnyEventRaised += OnAnyEventRaised;

            if (eventSource is IEventSource2 eventSource2)
            {
                eventSource2.TelemetryLogged += OnTelemetryLogged;
            }

            foreach (ILogger logger in Loggers)
            {
                logger.Initialize(this);
            }

            Dispatch(new BuildStartedEventArgs("Build Started", null, environmentOfBuild: Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(i => (string)i.Key).ToDictionary(i => (string)i.Key, i => (string)i.Value)));
        }

        /// <inheritdoc />
        public void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0) => Dispatch(new BuildErrorEventArgs(subcategory: null, code: code, file: file ?? "SlnGen", lineNumber: lineNumber, columnNumber: columnNumber, endLineNumber: 0, endColumnNumber: 0, message: message, helpKeyword: null, senderName: null));

        /// <inheritdoc />
        public void LogEvent(BuildEventArgs eventArgs)
        {
            Dispatch(eventArgs);
        }

        /// <inheritdoc />
        public void LogMessageHigh(string message, params object[] args) => Dispatch(new BuildMessageEventArgs(message, null, null, MessageImportance.High, DateTime.UtcNow, args));

        /// <inheritdoc />
        public void LogMessageLow(string message, params object[] args) => Dispatch(new BuildMessageEventArgs(message, null, null, MessageImportance.Low, DateTime.UtcNow, args));

        /// <inheritdoc />
        public void LogMessageNormal(string message, params object[] args) => Dispatch(new BuildMessageEventArgs(message, null, null, MessageImportance.Normal, DateTime.UtcNow, args));

        /// <inheritdoc />
        public void LogTelemetry(string eventName, IDictionary<string, string> properties) => OnTelemetryLogged(this, new TelemetryEventArgs { EventName = eventName, Properties = properties });

        /// <inheritdoc />
        public void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0) => Dispatch(new BuildWarningEventArgs(subcategory: null, code: code, file: file ?? "SlnGen", lineNumber: lineNumber, columnNumber: columnNumber, endLineNumber: 0, endColumnNumber: 0, message: message, helpKeyword: null, senderName: null));

        /// <inheritdoc />
        public void Shutdown()
        {
            Dispatch(new BuildFinishedEventArgs(HasLoggedErrors ? "Failed" : "Success", null, !HasLoggedErrors));

            foreach (ILogger logger in Loggers)
            {
                logger.Shutdown();
            }

            _eventSource.AnyEventRaised -= OnAnyEventRaised;

            if (_eventSource is IEventSource2 eventSource2)
            {
                eventSource2.TelemetryLogged -= OnTelemetryLogged;
            }
        }

        private new void Dispatch(BuildEventArgs e)
        {
            if (_noWarn && e is BuildWarningEventArgs)
            {
                return;
            }

            if (_hasLoggedErrors == 0 && e is BuildErrorEventArgs)
            {
                Interlocked.Exchange(ref _hasLoggedErrors, 1);
            }

            if (e.BuildEventContext == null)
            {
                e.BuildEventContext = BuildEventContext.Invalid;
            }

            base.Dispatch(e);
        }

        private void OnAnyEventRaised(object sender, BuildEventArgs e)
        {
            Dispatch(e);
        }

        private void OnTelemetryLogged(object sender, TelemetryEventArgs e) => TelemetryLogged?.Invoke(sender, e);
    }
}