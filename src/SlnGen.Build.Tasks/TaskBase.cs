// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// A base class for all MSBuild tasks.
    /// </summary>
    public abstract class TaskBase : ITask
    {
        /// <inheritdoc cref="ITask.BuildEngine"/>
        public IBuildEngine BuildEngine { get; set; }

        /// <inheritdoc cref="ITask.HostObject"/>
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// Gets a value indicating whether any errors have been logged.
        /// </summary>
        protected bool HasLoggedErrors { get; private set; }

        /// <inheritdoc cref="ITask.Execute"/>
        public bool Execute()
        {
            ExecuteTask();

            return !HasLoggedErrors;
        }

        /// <summary>
        /// Logs an error for a task.
        /// </summary>
        public void LogError(string message, string code = null, bool includeLocation = false)
        {
            HasLoggedErrors = true;

            BuildEngine?.LogErrorEvent(new BuildErrorEventArgs(
                subcategory: null,
                code: code,
                file: includeLocation ? BuildEngine?.ProjectFileOfTaskNode : null,
                lineNumber: includeLocation ? (int)BuildEngine?.LineNumberOfTaskNode : 0,
                columnNumber: includeLocation ? (int)BuildEngine?.ColumnNumberOfTaskNode : 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null));
        }

        /// <summary>
        /// Logs a high importance message.
        /// </summary>
        public void LogMessageHigh(string message, params object[] args)
        {
            BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(
                subcategory: null,
                code: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null,
                importance: MessageImportance.High,
                eventTimestamp: DateTime.Now,
                messageArgs: args));
        }

        /// <summary>
        /// Logs a low importance message.
        /// </summary>
        public void LogMessageLow(string message, params object[] args)
        {
            BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(
                subcategory: null,
                code: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null,
                importance: MessageImportance.Low,
                eventTimestamp: DateTime.MaxValue,
                messageArgs: args));
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        public void LogMessageNormal(string message, params object[] args)
        {
            BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(
                subcategory: null,
                code: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null,
                importance: MessageImportance.Normal,
                eventTimestamp: DateTime.Now,
                messageArgs: args));
        }

        /// <summary>
        /// Logs a warning.
        /// </summary>
        public void LogWarning(string message, string code = null, bool includeLocation = false)
        {
            BuildEngine?.LogWarningEvent(new BuildWarningEventArgs(
                subcategory: null,
                code: code,
                file: includeLocation ? BuildEngine?.ProjectFileOfTaskNode : null,
                lineNumber: includeLocation ? (int)BuildEngine?.LineNumberOfTaskNode : 0,
                columnNumber: includeLocation ? (int)BuildEngine?.ColumnNumberOfTaskNode : 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null));
        }

        /// <summary>
        /// Executes the logic of the task.
        /// </summary>
        protected abstract void ExecuteTask();
    }
}