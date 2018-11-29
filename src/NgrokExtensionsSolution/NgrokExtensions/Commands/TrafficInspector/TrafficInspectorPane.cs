namespace NgrokExtensions.TrafficInspector
{
    using DrWPF.Windows.Data;
    using Microsoft.VisualStudio.Shell;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NgrokExtensions.Services;
    using System;
    using System.IO;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

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
    public class TrafficInspectorPane : ToolWindowPane
    {
        public const string WindowGuidString = "e45179e2-3f44-4c96-8c5a-9bcb135127d4";
        public const string Title = "Traffic Inspector";

        //private IServiceProvider _serviceProvider;
        ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokTrafficInspector"/> class.
        /// </summary>
        public TrafficInspectorPane(ILoggerService l) : base(null)
        {
            _logger = l;
            //_serviceProvider = sp;

            //logger = _serviceProvider.GetService(typeof(ILogger)) as ILogger;

            //IMenuCommandService mcs = Package.GetService(typeof(IMenuCommandService)) as IMenuCommandService;

            this.Caption = Title;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var trafficInspectorControl = new TrafficInspectorControl
            {
                //DataContext = roundtrips
            };

            this.Content = trafficInspectorControl;
        }

        protected async override void OnCreate()
        {
            base.OnCreate();

        }

    }
}
