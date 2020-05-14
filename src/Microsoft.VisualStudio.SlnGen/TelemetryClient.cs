// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a class used for logging telemetry.
    /// </summary>
    internal sealed class TelemetryClient : IDisposable
    {
        private readonly TelemetrySession _telemetrySession;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient"/> class.
        /// </summary>
        public TelemetryClient()
        {
            // Only enable telemetry if the user has opted into it in Visual Studio
            TelemetryService.DefaultSession.UseVsIsOptedIn();

            if (TelemetryService.DefaultSession.IsOptedIn)
            {
                _telemetrySession = TelemetryService.DefaultSession;

                GitRepositoryInfo repositoryInfo = GitRepositoryInfo.GetRepoInfoForCurrentDirectory();

                if (repositoryInfo?.Origin != null)
                {
                    TelemetryContext context = _telemetrySession.CreateContext("GitRepository");

                    context.SharedProperties["VS.TeamFoundation.Git.OriginRemoteUrlHashV2"] = new TelemetryPiiProperty(repositoryInfo.Origin);
                }
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _telemetrySession?.Dispose();
        }

        /// <summary>
        /// Posts an event to the telemetry pipeline if available.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="properties">An <see cref="IDictionary{TKey,TValue}" /> containing the event properties.</param>
        /// <param name="piiProperties">An <see cref="IDictionary{TKey,TValue}" /> containing the event properties containing personally identifiable information (PII).</param>
        /// <returns><code>true</code> if the event was successfully posted, otherwise <code>false</code>.</returns>
        public bool PostEvent(string name, IDictionary<string, object> properties, IDictionary<string, object> piiProperties = null)
        {
            if (_telemetrySession == null)
            {
                return false;
            }

            TelemetryEvent telemetryEvent = new TelemetryEvent(name);

            foreach (KeyValuePair<string, object> property in properties)
            {
                telemetryEvent.Properties[property.Key] = property.Value;
            }

            if (piiProperties != null)
            {
                foreach (KeyValuePair<string, object> property in piiProperties)
                {
                    if (property.Value != null)
                    {
                        telemetryEvent.Properties[property.Key] = new TelemetryPiiProperty(property.Value);
                    }
                }
            }

            _telemetrySession.PostEvent(telemetryEvent);

            return true;
        }

        /// <summary>
        /// Posts an exception event to the telemetry pipeline if available.
        /// </summary>
        /// <param name="exception">The <see cref="Exception" /> that occured.</param>
        /// <returns><code>true</code> if the event was successfully posted, otherwise <code>false</code>.</returns>
        public bool PostException(Exception exception)
        {
            if (_telemetrySession == null)
            {
                return false;
            }

            _telemetrySession.PostFault("slngen/exception", string.Empty, FaultSeverity.Critical, exception);

            return true;
        }
    }
}