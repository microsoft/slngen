// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using SlnGen.Common;

namespace SlnGen.Build.Tasks
{
    internal class TaskLogger : ISlnGenLogger
    {
        private readonly TaskBase _task;

        public TaskLogger(TaskBase task)
        {
            _task = task;
        }

        public void LogError(string message, string code = null, bool includeLocation = false) => _task.LogError(message, code, includeLocation);

        public void LogMessageHigh(string message, params object[] args) => _task.LogMessageHigh(message, args);

        public void LogMessageLow(string message, params object[] args) => _task.LogMessageLow(message, args);

        public void LogMessageNormal(string message, params object[] args) => _task.LogMessageNormal(message, args);

        public void LogWarning(string message, string code = null, bool includeLocation = false) => _task.LogWarning(message, code, includeLocation);
    }
}