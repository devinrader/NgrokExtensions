using Microsoft;
using Microsoft.VisualStudio.Shell;
using NgrokExtensions.Services;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions.TunnelInspector
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TunnelInspectorCommand
    {
        public const int CommandId = 4129;
        public static readonly Guid CommandSet = new Guid("30d1a36d-a03a-456d-b639-f28b9b23e161");

        private static IProcessManagerService processManagerService;
        private static ITunnelManagerService tunnelManagerService;

        public TunnelInspectorCommand(AsyncPackage package, IMenuCommandService commandService, IProcessManagerService processManager, ITunnelManagerService tunnelManager)
        {
            processManagerService = processManager;
            tunnelManagerService = tunnelManager;

            var menuCommandID = new CommandID(CommandSet, CommandId);

            var menuItem = new OleMenuCommand((s, e) => Execute(package), menuCommandID);
            //menuItem.BeforeQueryStatus += MenuItem_BeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Initializes the instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TunnelInspectorCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            IProcessManagerService processManagerService = await package.GetServiceAsync(typeof(SProcessManagerService)) as IProcessManagerService;
            ITunnelManagerService tunnelManagerService = await package.GetServiceAsync(typeof(STunnelManagerService)) as ITunnelManagerService;

            if (processManagerService.StartedPriorToExtensionInit) {
                await tunnelManagerService.InitializeTunnelsAsync();
            }

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;

            new TunnelInspectorCommand(package, commandService, processManagerService, tunnelManagerService);
        }

        //private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        //{
        //    MenuCommand command = (MenuCommand)sender;

        //    command.Enabled = true;
        //    command.Visible = true;
        //    command.Checked = tunnelManagerService.IsMonitoring;
        //}

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        //private void Execute(object sender, EventArgs e)
        private void Execute(AsyncPackage package)
        {
            package.JoinableTaskFactory.RunAsync(async () =>
            {
                ToolWindowPane window = await package.ShowToolWindowAsync(
                    typeof(TunnelInspectorPane),
                    0,
                    create: true,
                    cancellationToken: package.DisposalToken);
            }).FileAndForget("NgrokExtensions/TunnelInspectorPane/Open");
        }
    }
}
