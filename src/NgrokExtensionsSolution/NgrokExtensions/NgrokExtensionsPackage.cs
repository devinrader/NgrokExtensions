// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Copyright (c) 2016 David Prothero

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NgrokExtensions.Services;
using NgrokExtensions.TrafficInspector;
using NgrokExtensions.TunnelInspector;

namespace NgrokExtensions
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(NgrokExtensionsPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionsPageGrid), "ngrok", "Options", 0, 0, true)]
    [ProvideToolWindow(typeof(NgrokExtensions.TrafficInspector.TrafficInspectorPane), Style=VsDockStyle.Float, Transient =true )]
    [ProvideToolWindow(typeof(NgrokExtensions.TunnelInspector.TunnelInspectorPane), Style =VsDockStyle.Float, Transient =true )]
    [ProvideService(typeof(SLoggerService))]
    [ProvideService(typeof(SRepositoryService))]
    [ProvideService(typeof(SProcessManagerService))]
    [ProvideService(typeof(SWebApplicationsManagerService))]
    [ProvideService(typeof(STunnelManagerService))]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class NgrokExtensionsPackage : AsyncPackage
    {
        /// <summary>
        /// StartTunnelPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "9f845cfc-84ef-4aac-9826-d46a83744fb4";

        public string ExecutablePath
        {
            get
            {
                OptionsPageGrid page = (OptionsPageGrid)this.GetDialogPage(typeof(OptionsPageGrid));
                return page.ExecutablePath;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartTunnelOld"/> class.
        /// </summary>
        public NgrokExtensionsPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.            
            IAsyncServiceContainer serviceContainer = this;
            serviceContainer.AddService(typeof(SLoggerService), CreateLoggerServiceAsync, true);
            serviceContainer.AddService(typeof(SRepositoryService), CreateRepositoryServiceAsync, true);
            serviceContainer.AddService(typeof(SProcessManagerService), CreateProcessManagerServiceAsync, true);
            serviceContainer.AddService(typeof(SWebApplicationsManagerService), CreateWebApplicationsManagerServiceAsync, true);
            serviceContainer.AddService(typeof(STunnelManagerService), CreateTunnelManagerServiceAsync, true);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        //protected override void Initialize()
        //{
        //    //StartTunnel.Initialize(this);
        //    base.Initialize();
        //}

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            IProcessManagerService processManagerService = await this.GetServiceAsync(typeof(SProcessManagerService)) as IProcessManagerService;
            processManagerService.StartedPriorToExtensionInit = processManagerService.IsRunning;

            DTE2 applicationObject = await this.GetServiceAsync(typeof(DTE)) as EnvDTE80.DTE2;
            DTEEvents dteEvents = applicationObject.Events.DTEEvents;
            dteEvents.OnBeginShutdown += DteEvents_OnBeginShutdown;

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await NgrokExtensions.TrafficInspector.TrafficInspectorCommand.InitializeAsync(this);
            await NgrokExtensions.Tunnels.StartTunnelCommand.InitializeAsync(this);
            await NgrokExtensions.TunnelInspector.TunnelInspectorCommand.InitializeAsync(this);
        }

        private async void DteEvents_OnBeginShutdown()
        {
            IProcessManagerService processManagerService = await this.GetServiceAsync(typeof(SProcessManagerService)) as IProcessManagerService;
            if (!processManagerService.StartedPriorToExtensionInit)
            {
                await processManagerService.StopAsync();
            }
        }

        #region Create Services

        /// <summary>
        /// This is the function that will create a new instance of the services the first time a client
        /// will ask for a specific service type. It is called by the base class's implementation of
        /// IAsyncServiceProvider.
        /// </summary>
        /// <param name="container">The IAsyncServiceContainer that needs a new instance of the service.
        ///                         This must be this package.</param>
        /// <param name="serviceType">The type of service to create.</param>
        /// <returns>The instance of the service.</returns>
        private async Task<object> CreateLoggerServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(SLoggerService).IsEquivalentTo(serviceType))
            {
                LoggerService service = new LoggerService(this);
                await service.InitializeAsync(cancellationToken);
                return service;
            }

            Debug.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        private async Task<object> CreateRepositoryServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(SRepositoryService).IsEquivalentTo(serviceType))
            {
                RepositoryService service = new RepositoryService(this);
//                await service.InitializeAsync(cancellationToken);
                return service;
            }

            Debug.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        private async Task<object> CreateProcessManagerServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(SProcessManagerService).IsEquivalentTo(serviceType))
            {
                ProcessManagerService service = new ProcessManagerService(this);
                await service.InitializeAsync(cancellationToken);
                return service;
            }

            Debug.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        private async Task<object> CreateWebApplicationsManagerServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(SWebApplicationsManagerService).IsEquivalentTo(serviceType))
            {
                WebApplicationsMangerService service = new WebApplicationsMangerService(this);
                await service.InitializeAsync(cancellationToken);
                return service;
            }

            Debug.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        private async Task<object> CreateTunnelManagerServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            if (typeof(STunnelManagerService).IsEquivalentTo(serviceType))
            {
                TunnelManagerService service = new TunnelManagerService(this);
                await service.InitializeAsync(cancellationToken);
                return service;
            }

            Debug.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        #endregion

        #region ToolWindow Overrides

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType.Equals(new Guid(TunnelInspectorPane.WindowGuidString)))
            {
                return this;
            }

            if (toolWindowType.Equals(new Guid(TrafficInspectorPane.WindowGuidString)))
            {
                return this;
            }

            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(TunnelInspectorPane))
            {
                return TunnelInspectorPane.Title;
            }

            if (toolWindowType == typeof(TrafficInspectorPane))
            {
                return TrafficInspectorPane.Title;
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected async override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;

            IRepositoryService repositoryService = await GetServiceAsync(typeof(SRepositoryService), false) as IRepositoryService;
            ILoggerService loggerService = await GetServiceAsync(typeof(SLoggerService), false) as ILoggerService;
            ITunnelManagerService tunnelManagerService = await GetServiceAsync(typeof(STunnelManagerService), false) as ITunnelManagerService;

            return new TunnelInspectorPaneState
            {
                DTE = dte,
                TunnelManager = tunnelManagerService,
                Logger = loggerService
            };
        }

        #endregion
    }
}
