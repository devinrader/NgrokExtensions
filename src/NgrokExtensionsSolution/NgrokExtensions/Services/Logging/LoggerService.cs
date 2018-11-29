using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using Task = System.Threading.Tasks.Task;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using IVsOutputWindowPane = Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane;
using IVsOutputWindow = Microsoft.VisualStudio.Shell.Interop.IVsOutputWindow;
using SVsOutputWindow = Microsoft.VisualStudio.Shell.Interop.SVsOutputWindow;
using System.Diagnostics;

namespace NgrokExtensions.Services
{
    public class LoggerService : SLoggerService, ILoggerService
    {
        private IAsyncServiceProvider serviceProvider;
        private IVsOutputWindow output;
        private IVsOutputWindowPane pane;

        private readonly string _name = "ngrok";

        public LoggerService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            output = await serviceProvider.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Assumes.Present(output);
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane.OutputString(System.String)")]
        public async Task WriteToOutputWindowAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (EnsurePane())
                {
                    pane.OutputStringThreadSafe(message);
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane.OutputString(System.String)")]
        public async Task WriteLineToOutputWindowAsync(string message)
        {
            await WriteToOutputWindowAsync(message + Environment.NewLine);
        }

        //public async Task LogAsync(Exception ex)
        //{
        //    if (ex != null)
        //    {
        //        await LogToOutputWindowAsync(ex.ToString());
        //    }
        //}

        private bool EnsurePane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (pane == null)
            {
                var guid = Guid.NewGuid();
                output.CreatePane(ref guid, _name, 1, 1);
                output.GetPane(ref guid, out pane);
            }

            return pane != null;
        }
    }
}