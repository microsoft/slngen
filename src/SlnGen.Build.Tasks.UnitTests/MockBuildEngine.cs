// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SlnGen.Build.Tasks.UnitTests
{
    internal class MockBuildEngine : IBuildEngine5
    {
        private readonly List<BuildEventArgs> _events = new List<BuildEventArgs>();

        public int ColumnNumberOfTaskNode => 0;

        public bool ContinueOnError => false;

        public IReadOnlyCollection<BuildEventArgs> Events => _events;

        public bool IsRunningMultipleNodes => false;

        public int LineNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => null;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
        {
            throw new NotSupportedException();
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion)
        {
            throw new NotImplementedException();
        }

        public bool BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IDictionary[] targetOutputsPerProject, string[] toolsVersion, bool useResultsCache, bool unloadProjectsOnCompletion)
        {
            throw new NotSupportedException();
        }

        public BuildEngineResult BuildProjectFilesInParallel(string[] projectFileNames, string[] targetNames, IDictionary[] globalProperties, IList<string>[] removeGlobalProperties, string[] toolsVersion, bool returnTargetOutputs)
        {
            throw new NotSupportedException();
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotSupportedException();
        }

        public void LogCustomEvent(CustomBuildEventArgs e) => _events.Add(e);

        public void LogErrorEvent(BuildErrorEventArgs e) => _events.Add(e);

        public void LogMessageEvent(BuildMessageEventArgs e) => _events.Add(e);

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            throw new NotSupportedException();
        }

        public void LogWarningEvent(BuildWarningEventArgs e) => _events.Add(e);

        public void Reacquire()
        {
            throw new NotSupportedException();
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            throw new NotSupportedException();
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            throw new NotSupportedException();
        }

        public void Yield()
        {
            throw new NotSupportedException();
        }
    }
}