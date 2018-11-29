using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using EnvDTE80;
using System.ComponentModel.Composition;
using NgrokExtensions.Services;
using System.Diagnostics;

namespace NgrokExtensions.Tunnels
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class StartTunnelCommand
    {
 
        public const int CommandId = 4131;
        public static readonly Guid CommandSet = new Guid("30d1a36d-a03a-456d-b639-f28b9b23e161");

        private ILoggerService loggerService;
        private IProcessManagerService processManagerService;
        private IWebApplicationsManagerService webApplicationsManagerService;
        private ITunnelManagerService tunnelManagerService;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartTunnelCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private StartTunnelCommand(AsyncPackage package, OleMenuCommandService commandService, ILoggerService logger, IProcessManagerService processManager, IWebApplicationsManagerService webApplicationsManager, ITunnelManagerService tunnelManager)
        {
            loggerService = logger;
            processManagerService = processManager;
            webApplicationsManagerService = webApplicationsManager;
            tunnelManagerService = tunnelManager;

            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);

            OleMenuCommand startTunnelToggleButtonMenuCommand = new OleMenuCommand(this.Execute, menuCommandID);
            startTunnelToggleButtonMenuCommand.BeforeQueryStatus += StartTunnelToggleButtonMenuCommand_BeforeQueryStatus;
            commandService.AddCommand(startTunnelToggleButtonMenuCommand);
        }

        private void StartTunnelToggleButtonMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            MenuCommand command = (MenuCommand)sender;

            Debug.WriteLine($"BeforeQueryStatus: Checked: {command.Checked}, IsMonitoring: {tunnelManagerService.IsMonitoring}");

            //command.Enabled = true;
            //command.Visible = true;
            command.Checked = tunnelManagerService.IsMonitoring;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in StartTunnelCommand's constructor requires
            // the UI thread.
            //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            ILoggerService loggerService = await package.GetServiceAsync(typeof(SLoggerService), false) as ILoggerService;
            IProcessManagerService processMangerService = await package.GetServiceAsync(typeof(SProcessManagerService), false) as IProcessManagerService;
            IWebApplicationsManagerService webApplicationsMangerService = await package.GetServiceAsync(typeof(SWebApplicationsManagerService), false) as IWebApplicationsManagerService;
            ITunnelManagerService tunnelManagerService = await package.GetServiceAsync(typeof(STunnelManagerService), false) as ITunnelManagerService;
            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;

            new StartTunnelCommand(package, commandService, loggerService, processMangerService, webApplicationsMangerService, tunnelManagerService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                if (!tunnelManagerService.IsMonitoring)
                {
                    await StartMonitorAsync();
                }
                else
                {
                    await StopMonitorAsync();
                }
            });
        }

        private async Task StartMonitorAsync()
        {
            await loggerService.WriteLineToOutputWindowAsync("Starting ngrok monitor...");

            if (!processManagerService.IsRunning)
            {
                if (webApplicationsManagerService.Projects.Count == 0)
                {
                    await loggerService.WriteLineToOutputWindowAsync("Did not find any web projects to monitor in this solution.  Shutting down.");
                    return;
                }

                var installPlease = false;

                if (!(await processManagerService.VerifyExecutableInstalledAsync()))
                {
                    await loggerService.WriteLineToOutputWindowAsync("not found");

                    if (AskUserYesNoQuestion("ngrok could not be located. Would you like me to download it from ngrok.com?"))
                    {
                        installPlease = true;
                    }
                    else
                    {
                        return;
                    }
                }

                if (installPlease)
                {
                    await loggerService.WriteToOutputWindowAsync("Attempting install...");

                    try
                    {
                        var installer = new NgrokInstaller();
                        var result = await installer.InstallAsync();

                        if (result.Success)
                        {
                            await loggerService.WriteLineToOutputWindowAsync("successful.");
                            processManagerService.ExecutablePath = result.ExecutablePath;
                        }
                    }
                    catch (NgrokDownloadException ngrokDownloadException)
                    {
                        await loggerService.WriteLineToOutputWindowAsync("failed.");
                        await loggerService.WriteLineToOutputWindowAsync(ngrokDownloadException.Message);
                        return;
                    }
                }

                await processManagerService.StartAsync();
            }

            await tunnelManagerService.InitializeTunnelsAsync();

            //start websocket monitoring

            tunnelManagerService.IsMonitoring = true;
        }

        private async Task StopMonitorAsync()
        {
            //remove tunnels fron ngrok

            tunnelManagerService.IsMonitoring = false;
        }

        private bool AskUserYesNoQuestion(string message)
        {
            var result = VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                "ngrok",
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            return result == 6;  // Yes
        }

    }
}
