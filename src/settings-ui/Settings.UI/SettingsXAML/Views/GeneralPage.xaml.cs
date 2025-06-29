// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Flyout;
using Microsoft.PowerToys.Settings.UI.Helpers;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Data.Json;

namespace Microsoft.PowerToys.Settings.UI.Views
{
    /// <summary>
    /// General Settings Page.
    /// </summary>
    public sealed partial class GeneralPage : Page, IRefreshablePage
    {
        private static DateTime OkToHideBackupAndRestoreMessageTime { get; set; }

        /// <summary>
        /// Gets or sets view model.
        /// </summary>
        public GeneralViewModel ViewModel { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralPage"/> class.
        /// General Settings page constructor.
        /// </summary>
        public GeneralPage()
        {
            InitializeComponent();

            // Load string resources
            var loader = Helpers.ResourceLoaderInstance.ResourceLoader;
            var settingsUtils = new SettingsUtils();

            Action stateUpdatingAction = () =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ViewModel.RefreshUpdatingState();
                });
            };

            Action hideBackupAndRestoreMessageArea = () =>
            {
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    const int messageShowTimeIs = 10000;

                    // in order to keep the message for about 5 seconds after the last call
                    // and not need any lock/thread-synch, use an OK-To-Hide time, and wait just a little longer than that.
                    OkToHideBackupAndRestoreMessageTime = DateTime.UtcNow.AddMilliseconds(messageShowTimeIs - 16);
                    await System.Threading.Tasks.Task.Delay(messageShowTimeIs);
                    if (DateTime.UtcNow > OkToHideBackupAndRestoreMessageTime)
                    {
                        ViewModel.HideBackupAndRestoreMessageArea();
                    }
                });
            };

            var doRefreshBackupRestoreStatus = new Action<int>(RefreshBackupRestoreStatus);

            ViewModel = new GeneralViewModel(
                SettingsRepository<GeneralSettings>.GetInstance(settingsUtils),
                loader.GetString("GeneralSettings_RunningAsAdminText"),
                loader.GetString("GeneralSettings_RunningAsUserText"),
                ShellPage.IsElevated,
                ShellPage.IsUserAnAdmin,
                ShellPage.SendDefaultIPCMessage,
                ShellPage.SendRestartAdminIPCMessage,
                ShellPage.SendCheckForUpdatesIPCMessage,
                string.Empty,
                stateUpdatingAction,
                hideBackupAndRestoreMessageArea,
                doRefreshBackupRestoreStatus,
                PickSingleFolderDialog,
                loader);

            DataContext = ViewModel;

            ViewModel.InitializeReportBugLink();

            // Register IPC handler for bug report status
            ShellPage.ShellHandler.IPCResponseHandleList.Add(HandleBugReportStatusResponse);            // Register cleanup on unload
            this.Unloaded += GeneralPage_Unloaded;

            CheckBugReportStatus();

            doRefreshBackupRestoreStatus(100);
        }

        private void OpenColorsSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helpers.StartProcessHelper.Start(Helpers.StartProcessHelper.ColorsSettings);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error while trying to open the system color settings", ex);
            }
        }

        private void OpenDiagnosticsAndFeedbackSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Helpers.StartProcessHelper.Start(Helpers.StartProcessHelper.DiagnosticsAndFeedback);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error while trying to open the system Diagnostics & Feedback settings", ex);
            }
        }

        private void RefreshBackupRestoreStatus(int delayMs = 0)
        {
            Task.Run(() =>
            {
                if (delayMs > 0)
                {
                    Thread.Sleep(delayMs);
                }

                var settingsBackupAndRestoreUtils = SettingsBackupAndRestoreUtils.Instance;
                var results = settingsBackupAndRestoreUtils.DryRunBackup();
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ViewModel.NotifyAllBackupAndRestoreProperties();
                });
            });
        }

        private void UpdateBackupAndRestoreStatusText(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            RefreshBackupRestoreStatus();
        }

        private async Task<string> PickSingleFolderDialog()
        {
            // This function was changed to use the shell32 API to open folder dialog
            // as the old one (PickSingleFolderAsync) can't work when the process is elevated
            // TODO: go back PickSingleFolderAsync when it's fixed
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.GetSettingsWindow());
            string r = await Task.FromResult<string>(ShellGetFolder.GetFolderDialog(hwnd));
            return r;
        }

        private void Click_LanguageRestart(object sender, RoutedEventArgs e)
        {
            ViewModel.Restart();
        }

        private void Click_ViewDiagnosticDataViewerRestart(object sender, RoutedEventArgs e)
        {
            ViewModel.Restart();
        }

        public void RefreshEnabledState()
        {
            ViewModel.RefreshSettingsOnExternalChange();
        }

        private async void ViewDiagnosticData_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(ViewModel.ViewDiagnosticData);
        }

        private void ExitPTItem_Tapped(object sender, RoutedEventArgs e)
        {
            const string ptTrayIconWindowClass = "PToyTrayIconWindow"; // Defined in runner/tray_icon.h
            const nuint ID_EXIT_MENU_COMMAND = 40001;                  // Generated resource from runner/runner.base.rc

            // Exit the XAML application
            Application.Current.Exit();

            // Invoke the exit command from the tray icon
            IntPtr hWnd = NativeMethods.FindWindow(ptTrayIconWindowClass, ptTrayIconWindowClass);
            NativeMethods.SendMessage(hWnd, NativeMethods.WM_COMMAND, ID_EXIT_MENU_COMMAND, 0);
        }

        private void BugReportToolClicked(object sender, RoutedEventArgs e)
        {
            // Start bug report
            var launchPage = new LaunchPage();
            launchPage.ReportBugBtn_Click(sender, e);

            ViewModel.IsBugReportRunning = true;

            // No need to start timer - the observer pattern will notify us when it finishes
        }

        private void CheckBugReportStatus()
        {
            // Send one-time request to check current bug report status
            string ipcMessage = "{ \"bug_report_status\": { } }";
            ShellPage.SendDefaultIPCMessage(ipcMessage);
        }

        private void HandleBugReportStatusResponse(JsonObject response)
        {
            if (response.ContainsKey("bug_report_running"))
            {
                var isRunning = response.GetNamedBoolean("bug_report_running");

                // Update UI on the UI thread
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ViewModel.IsBugReportRunning = isRunning;
                });
            }
        }

        private void GeneralPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupBugReportHandlers();
        }

        private void CleanupBugReportHandlers()
        {
            // Remove IPC handler
            if (ShellPage.ShellHandler?.IPCResponseHandleList != null)
            {
                ShellPage.ShellHandler.IPCResponseHandleList.Remove(HandleBugReportStatusResponse);
            }
        }
    }
}
