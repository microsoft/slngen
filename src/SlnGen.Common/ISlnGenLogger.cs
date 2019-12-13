// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

namespace SlnGen.Common
{
    public interface ISlnGenLogger
    {
        void LogError(string message, string code = null, bool includeLocation = false);

        void LogMessageHigh(string message, params object[] args);

        void LogMessageLow(string message, params object[] args);

        void LogMessageNormal(string message, params object[] args);

        void LogWarning(string message, string code = null, bool includeLocation = false);
    }
}