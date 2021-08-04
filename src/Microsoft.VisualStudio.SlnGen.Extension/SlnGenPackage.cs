// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class SlnGenPackage : AsyncPackage
    {
        public const string PackageGuidString = "7f489eef-951a-410b-a5b8-777ee38447d5";

        /// <summary>
        /// The file extension of this project type.  No preceding period.
        /// </summary>
        public const string ProjectExtension = "proj";

        /// <summary>
        /// The GUID for this project type.  It is unique with the project file extension and appears under the VS registry hive's Projects key.
        /// </summary>
        public const string ProjectTypeGuid = "bcc30a4b-97b0-44b3-9611-88c85c275ca7";

        private SolutionEvents _solutionEvents;

        private int _isSolutionReloading;

        public uint SolutionCookie { get; set; }

        public async Task<int> ReloadSolutionAsync(CancellationToken cancellationToken)
        {
            if (_isSolutionReloading == 1 || Interlocked.Increment(ref _isSolutionReloading) != 1)
            {
                return 0;
            }

            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

                Assumes.Present(solution);

                List<IVsProject> projects = solution.GetProjects().ToList();

                if (projects.Count != 1)
                {
                    return 0;
                }

                string project = projects.Select(i => i.GetFullPath()).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));

                int result = solution.CloseSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0);

                ErrorHandler.ThrowOnFailure(result);

                result = await RunSlnGenAsync(project, cancellationToken);

                ErrorHandler.ThrowOnFailure(result);

                result = solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, Path.ChangeExtension(project, "sln"));

                ErrorHandler.ThrowOnFailure(result);

                if (SolutionCookie == default)
                {
                    _solutionEvents ??= new SolutionEvents(this);

                    result = solution.AdviseSolutionEvents(_solutionEvents, out uint cookie);

                    ErrorHandler.ThrowOnFailure(result);

                    SolutionCookie = cookie;
                }

                return result;
            }
            finally
            {
                Interlocked.Decrement(ref _isSolutionReloading);
            }
        }

        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            return ReloadSolutionAsync(cancellationToken);
        }

        private async Task<int> RunSlnGenAsync(string project, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            using SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

            using Process process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "slngen.exe",
                    Arguments = string.Join(
                        " ",
                        $"\"{project}\"",
                        "--launch:false"),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                },
            };

            process.Exited += (_, _) => semaphore.Release();

            process.Start();

            try
            {
                await semaphore.WaitAsync(cancellationToken);

                return process.ExitCode;
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
        }
    }
}