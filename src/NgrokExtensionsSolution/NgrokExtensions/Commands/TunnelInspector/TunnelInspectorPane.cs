using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using NgrokExtensions.Services;

namespace NgrokExtensions.TunnelInspector
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid(WindowGuidString)]
    public class TunnelInspectorPane : ToolWindowPane
    {
        public const string WindowGuidString = "77ce76db-f497-4c3a-8110-16e11b11db3f";
        public const string Title = "Tunnel Inspector";

        /// <summary>
        /// Initializes a new instance of the <see cref="TunnelInspector"/> class.
        /// </summary>
        public TunnelInspectorPane(TunnelInspectorPaneState state) : base(null)
        {
            this.Caption = Title;

            this.ToolBar = new CommandID(new Guid("30d1a36d-a03a-456d-b639-f28b9b23e161"), 0x1000);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var tunnelInspectorControl = new TunnelInspectorControl
            {
                DataContext = state
            };

            this.Content = tunnelInspectorControl;
        }
    }
}
