﻿// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents information about a Git repository.
    /// </summary>
    internal sealed class GitRepositoryInfo
    {
        /// <summary>
        /// Gets the <see cref="Uri" /> of the remote named &quot;origin&quot; if one exists, otherwise <code>null</code>.
        /// </summary>
        public Uri Origin { get; private set; }

        /// <summary>
        /// Gets the information for the current Git repository if available.
        /// </summary>
        /// <returns>A <see cref="GitRepositoryInfo" /> object if the current directory is in a Git repository, otherwise <code>null</code>.</returns>
        public static GitRepositoryInfo GetRepoInfoForCurrentDirectory()
        {
            return TryGetRemoteUri(out Uri remoteUri)
                ? new GitRepositoryInfo
                {
                    Origin = remoteUri,
                }
                : null;
        }

        /// <summary>
        /// Strips the Username and Password from the specified <see cref="Uri" />
        /// </summary>
        /// <param name="uri">The <see cref="Uri" /> to strip the username and password from.</param>
        /// <returns>A <see cref="Uri" /> without the username and password.</returns>
        public static Uri StripUsernameAndPassword(Uri uri)
        {
            return uri.UserInfo.IsNullOrWhiteSpace()
                ? uri
                : new UriBuilder(uri)
                {
                    Password = string.Empty,
                    UserName = string.Empty,
                }.Uri;
        }

        /// <summary>
        /// Gets the Git URL for the specified remote.
        /// </summary>
        /// <param name="remoteUri">Receives the <see cref="Uri" /> of the remote if available.</param>
        /// <param name="name">The optional name of the remote. Default value is &quot;origin&quot;.</param>
        /// <returns><code>true</code> if the remote URL could be determined, otherwise <code>false</code>.</returns>
        private static bool TryGetRemoteUri(out Uri remoteUri, string name = "origin")
        {
            remoteUri = null;

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $"remote get-url {name}",
                    FileName = "git",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                },
            };

            try
            {
                if (!process.Start() || !process.WaitForExit((int)TimeSpan.FromSeconds(5).TotalMilliseconds))
                {
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd().Trim();

                if (!output.IsNullOrWhiteSpace() && Uri.TryCreate(output, UriKind.Absolute, out remoteUri))
                {
                    remoteUri = StripUsernameAndPassword(remoteUri);

                    return true;
                }
            }
            catch (Exception)
            {
                // Ignored
            }

            return false;
        }
    }
}