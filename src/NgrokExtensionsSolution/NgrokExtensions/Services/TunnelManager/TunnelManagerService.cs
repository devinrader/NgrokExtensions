using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using NgrokExtensions.Models;
using NgrokExtensions.Tunnels;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions.Services
{
    public class TunnelManagerService : ITunnelManagerService, STunnelManagerService, INotifyPropertyChanged
    {
        private IAsyncServiceProvider serviceProvider;
        private IWebApplicationsManagerService webApplicationManager;
        private ILoggerService loggerService;

        private ObservableCollection<Tunnel> _tunnels = new ObservableCollection<Tunnel>();
        private bool _isMonitoring;

        private readonly HttpClient httpclient;

        public event PropertyChangedEventHandler PropertyChanged;

        public TunnelManagerService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;

            httpclient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            httpclient.BaseAddress = new Uri("http://localhost:4040");
            httpclient.DefaultRequestHeaders.Accept.Clear();
            httpclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            loggerService = await serviceProvider.GetServiceAsync(typeof(SLoggerService)) as ILoggerService;
            webApplicationManager = await serviceProvider.GetServiceAsync(typeof(SWebApplicationsManagerService)) as IWebApplicationsManagerService;
        }

        public ObservableCollection<Tunnel> Tunnels
        {
            get
            {
                return _tunnels;
            }
            set
            {
                if (value != this._tunnels)
                {
                    this._tunnels = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsMonitoring
        {
            get
            {
                return _isMonitoring;
            }
            set
            {
                if (value != this._isMonitoring)
                {
                    this._isMonitoring = value;
                    NotifyPropertyChanged();
                }
            }
        }


        public async Task InitializeTunnelsAsync()
        {
            // It may be possible that even if the Tunnels API endpoint is available that
            // ngrok will still return a 500 error when we try to create a new tunnel, 
            // if the ngrok session has not connected, so we might need to think
            // about a try policy here as well

            if (await VerifyApiReadinessAsync(10))
            {
                //get an initial list of existing tunnels since ngrok might have already been running with these projects configured
                var temporaryTunnelsList = await FetchTunnelsAsync();

                //run through projects in the solution and check to see if we find preconfigired tunnels matching their name
                foreach (var project in webApplicationManager.Projects)
                {
                    //any tunnel without at least one match we'll assume we need to create
                    if (!temporaryTunnelsList.Any(t=>t.name == project.Key))
                    {
                        await CreateTunnelAsync(project.Key, project.Value);
                    }
                }

                //once we are done with any tunnel creation needed, fetch the tunnels list from ngrok again and put into an observable collection so the UI updates
                //get an initial list of existing tunnels since ngrok might have already been running with these projects configured
                this.Tunnels = new ObservableCollection<Tunnel>(await FetchTunnelsAsync());
            }
        }

        private async Task<Tunnel[]> FetchTunnelsAsync()
        {
            var response = await httpclient.GetAsync("/api/tunnels");
            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();

                var listResponse = JsonConvert.DeserializeObject<ListResponse>(text);

                await loggerService.WriteLineToOutputWindowAsync($"Found the following tunnels:");
                await loggerService.WriteLineToOutputWindowAsync(string.Join(Environment.NewLine, listResponse.tunnels.Select(t => $"\t{t.public_url} -> {t.config.addr}")));

                return listResponse.tunnels;
            }

            return null;
        }

        private async Task CreateTunnelAsync(string projectName, WebApplication config)
        {
            var addr = $"localhost:{config.PortNumber}";

            var request = new ApiRequest
            {
                name = projectName,
                addr = addr,
                proto = "http",
                host_header = addr
            };

            if (!string.IsNullOrEmpty(config.SubDomain))
            {
                request.subdomain = config.SubDomain;
            }

            // API appears to return a 502 error with some details if the current ngrok session is closed 
            // May need to consider retries in that scenario, or maybe just fail nicely?

            // When you create an http tunnel via the API, two tunnels are actually created, 
            // one for HTTP amd one for HTTPS, but only one of those tunnels is returned in the 
            // response to the POST request, so anytime we're create a tunnel we have to make a second
            // API call to the list endpoint in order to refresh the tunnels list, unfortunately.

            var response = await httpclient.PostAsJsonAsync("/api/tunnels", request);

            if (response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                var tunnel = JsonConvert.DeserializeObject<Tunnel>(text);

                await loggerService.WriteLineToOutputWindowAsync($"Created tunnel for project {projectName} forwarding to {tunnel.config.addr}");
            }
            else
            {
                var text = await response.Content.ReadAsStringAsync();

                await loggerService.WriteLineToOutputWindowAsync($"Failed to create tunnel.  ngrok said: \n{response.StatusCode} errorText: '{text}'");
            }
        }

        private async Task<bool> VerifyApiReadinessAsync(int maxRetryAttempts)
        {
            bool retry = true;
            int retrycounter = 0;

            // Is Polly a better choice here for managing the retry?
            // Or is that overkill for what we need?

            while (retry)
            {
                try
                {
                    if (await CanConnectToApiAsync())
                    {
                        //if we can connect exit the loop and move on
                        await loggerService.WriteLineToOutputWindowAsync("API connection successful");
                        return true;
                    }
                }
                catch (WebException exc)
                {
                    //Sometimes the web server takes a moment to get started
                }

                if (retrycounter < maxRetryAttempts)
                {
                    retrycounter = retrycounter + 1;
                    await System.Threading.Tasks.Task.Delay(2000); //wait a couple of seconds before trying again                
                    await loggerService.WriteLineToOutputWindowAsync($"Retrying tunnel connection (attempt {retrycounter})...");
                }
                else
                {
                    await loggerService.WriteLineToOutputWindowAsync("Maximum service connection attempts reached. Could not connect to ngrok tunnels.");
                    retry = false;
                }
            }

            return false;
        }

        private async Task<bool> CanConnectToApiAsync()
        {
            try
            {
                // hoping that testing the /api endpoint here instead of the root server endpoint mitigates the 
                // weird timing issues that seem to occur because ngrok isn't ready yet?

                var response = await httpclient.GetAsync("/api");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }

            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }

            return false;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
