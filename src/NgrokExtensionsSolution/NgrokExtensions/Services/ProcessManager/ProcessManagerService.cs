using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions.Services
{
    public class ProcessManagerService : SProcessManagerService, IProcessManagerService
    {
        private IAsyncServiceProvider serviceProvider;
        private ILoggerService loggerService;

        private bool _startHidden = false;
        //private bool _isRunning = false;
        private int activeProcessId;

        public ProcessManagerService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;
        }

        public bool StartHidden
        {
            get { return _startHidden; }
            set { _startHidden = value;  }
        }

        public string ExecutablePath { get; set; }

        public string ExecutableName { get { return "ngrok.exe"; } }

        public string FullPath
        {
            get
            {
                if (!Path.HasExtension(ExecutablePath))
                {
                    return Path.Combine(ExecutablePath, ExecutableName);
                }
                else
                {
                    return ExecutablePath;
                }

            }
        }

        public bool IsRunning {
            get
            {
                // Do we care that we might want multiple process to run at once?
                return Process.GetProcessesByName("ngrok").Any();
            }
        }

        public bool StartedPriorToExtensionInit { get; set; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            loggerService = await serviceProvider.GetServiceAsync(typeof(SLoggerService)) as ILoggerService;

            var package = (AsyncPackage)serviceProvider;
            var page = (OptionsPageGrid)package.GetDialogPage(typeof(OptionsPageGrid));

            if (!string.IsNullOrWhiteSpace(page.ExecutablePath))
            {
                if (Path.HasExtension(page.ExecutablePath))
                {
                    this.ExecutablePath = Path.GetDirectoryName(page.ExecutablePath);
                }
                else
                {
                    this.ExecutablePath = page.ExecutablePath;
                }
            }

            this.StartHidden = false;
        }

        public async Task<bool> VerifyExecutableInstalledAsync()
        {
            await loggerService.WriteLineToOutputWindowAsync("Verifying ngrok executable is available. Searching the following locations:");

            List<string> searchpaths = new List<string>();

            if (!string.IsNullOrWhiteSpace(ExecutablePath))
            {
                searchpaths.Add(ExecutablePath);
            }

            var values = Environment.GetEnvironmentVariable("PATH") ?? "";
            searchpaths.AddRange(values.Split(Path.PathSeparator));

//            await loggerService.WriteLineToOutputWindowAsync(.Aggregate((current, next) => $"{current}{Environment.NewLine}\t{next}"));
            await loggerService.WriteLineToOutputWindowAsync(string.Join(Environment.NewLine, searchpaths.Select(s => $"\t{s}")));


            return searchpaths.Select(path => Path.Combine(path, ExecutableName)).Any(File.Exists);
        }

        public async Task StartAsync()
        {

            // Prevent duplicate processes from being started

            //TODO: Might want to think about detecting existing ngrok process running at extension init time
            // because it seems like in that scenario we shouldn't auto shut it down if VS closes

            if (!IsRunning)
            {
                await loggerService.WriteToOutputWindowAsync($"Starting process from path {FullPath}...");

                await StartAsync(FullPath);
            }
            else
            {
                activeProcessId = Process.GetProcessesByName("ngrok").First().Id;

                await loggerService.WriteLineToOutputWindowAsync($"Using existing ngrok process (pid: {activeProcessId})");
            }
        }

        private async Task StartAsync(string path)
        {
            var pi = new ProcessStartInfo(path, "start --none")
            {
                CreateNoWindow = false,
                WindowStyle = (StartHidden) ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };

            await StartAsync(pi);
        }

        private async Task StartAsync(ProcessStartInfo pi)
        {
            var process = Process.Start(pi);
            activeProcessId = process.Id;

            await loggerService.WriteLineToOutputWindowAsync("started");
        }

        public async Task StopAsync()
        {
            if (IsRunning)
            {
                if (!StartedPriorToExtensionInit)
                { 
                    var process = Process.GetProcessById(activeProcessId);
                    process.Kill();

                    await loggerService.WriteLineToOutputWindowAsync("Process stopped");
                }
            }
        }

    }
}
