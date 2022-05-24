// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.SlnGen.Extension
{
    /// <summary>
    /// Represents the main entry point for the package.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuidString)]
    public sealed class SlnGenPackage : AsyncPackage
    {
        /// <summary>
        /// Represents the package GUID as a string.
        /// </summary>
        public const string PackageGuidString = "7f489eef-951a-410b-a5b8-777ee38447d5";

        /// <summary>
        /// The file extension of this project type.  No preceding period.
        /// </summary>
        public const string ProjectExtension = "proj";

        /// <summary>
        /// The GUID for this project type.  It is unique with the project file extension and appears under the VS registry hive's Projects key.
        /// </summary>
        public const string ProjectTypeGuid = "bcc30a4b-97b0-44b3-9611-88c85c275ca7";

        /// <summary>
        /// Represents the output window pane identifier.
        /// </summary>
        private static readonly Guid OutputWindowPaneId = new Guid("{6F361660-4673-45FF-ACB7-99D8A9AE9853}");

        private int _isSolutionReloading;

        /// <inheritdoc />
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ReloadSolutionAsync(cancellationToken);
        }

        /// <summary>
        /// Activates the specified output window pane.
        /// </summary>
        /// <param name="paneId">The GUID of the pane.</param>
        /// <param name="name">The name of the pane.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to use.</param>
        /// <returns>The <see cref="IVsOutputWindowPane" /> of the window pane.</returns>
        private async Task<IVsOutputWindowPane> ActivateOutputWindowPaneAsync(Guid paneId, string name, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IVsOutputWindow outputWindow = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Assumes.Present(outputWindow);

            IVsUIShell uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;

            Assumes.Present(uiShell);

            if (!ErrorHandler.Succeeded(outputWindow.GetPane(paneId, out IVsOutputWindowPane outputPane)))
            {
                if (ErrorHandler.Failed(uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, VSConstants.StandardToolWindows.Output, out IVsWindowFrame windowFrame)))
                {
                    return null;
                }

                windowFrame.Show();

                outputWindow.CreatePane(paneId, name, fInitVisible: 1, fClearWithSolution: 0);

                outputWindow.GetPane(paneId, out outputPane);
            }

            outputPane.Activate();

            return outputPane;
        }

        private void HandleOpenSolution(object sender, EventArgs e)
        {
            Task.Run(() => ReloadSolutionAsync(CancellationToken.None)).FileAndForget("slngen/execute/generatesolution");
        }

        private async Task ReloadSolutionAsync(CancellationToken cancellationToken)
        {
            if (_isSolutionReloading == 1 || Interlocked.Increment(ref _isSolutionReloading) != 1)
            {
                return;
            }

            try
            {
                SolutionEvents.OnAfterOpenSolution -= HandleOpenSolution;

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

                Assumes.Present(solution);

                if (!solution.GetProperty<bool>(__VSPROPID.VSPROPID_IsSolutionOpen) || !solution.GetProperty<bool>(__VSPROPID.VSPROPID_IsSolutionDirty))
                {
                    return;
                }

                List<IVsProject> projects = solution.GetProjects().ToList();

                if (projects.Count != 1)
                {
                    return;
                }

                IVsOutputWindowPane outputWindowPane = await ActivateOutputWindowPaneAsync(OutputWindowPaneId, "SlnGen", cancellationToken);

                outputWindowPane?.Clear();

                string project = projects.Select(i => i.GetFullPath()).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));

                outputWindowPane?.OutputStringThreadSafe("Closing solution...");
                ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                outputWindowPane?.OutputStringThreadSafe($"Success{Environment.NewLine}");

                outputWindowPane?.OutputStringThreadSafe("Running SlnGen...");

                (int exitCode, string output) = await RunSlnGenAsync(project, cancellationToken);

                outputWindowPane?.OutputStringThreadSafe($"{(exitCode == 0 ? "Success" : "Failed!")}{Environment.NewLine}");

                outputWindowPane?.OutputStringThreadSafe(output);

                if (exitCode == 0)
                {
                    outputWindowPane?.OutputStringThreadSafe("Opening solution...");
                    ErrorHandler.ThrowOnFailure(solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, Path.ChangeExtension(project, "sln")));
                    outputWindowPane?.OutputStringThreadSafe($"Success{Environment.NewLine}");
                }

                SolutionEvents.OnAfterBackgroundSolutionLoadComplete += HandleOpenSolution;
            }
            finally
            {
                Interlocked.Decrement(ref _isSolutionReloading);
            }
        }

        private async Task<(int, string)> RunSlnGenAsync(string project, CancellationToken cancellationToken)
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
                    RedirectStandardOutput = true,
                },
            };

            process.Exited += (_, _) => semaphore.Release();

            process.Start();

            StringBuilder sb = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args?.Data != null)
                {
                    sb.AppendLine(args.Data);
                }
            };

            process.BeginOutputReadLine();

            try
            {
                await semaphore.WaitAsync(cancellationToken);

                return (process.ExitCode, sb.ToString());
            }
            catch (OperationCanceledException)
            {
                return (0, null);
            }
        }
    }
}