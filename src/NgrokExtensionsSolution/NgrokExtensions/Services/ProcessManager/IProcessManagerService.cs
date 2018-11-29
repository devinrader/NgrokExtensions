using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Services
{
    public interface IProcessManagerService
    {
        bool StartHidden { get; set; }

        string ExecutablePath { get; set; }

        string ExecutableName { get; }

        string FullPath { get; }

        bool IsRunning { get; /*set;*/ }

        bool StartedPriorToExtensionInit { get; set; }

        Task<bool> VerifyExecutableInstalledAsync();

        Task StartAsync();

        Task StopAsync();
    }

    public interface SProcessManagerService
    {

    }
}
