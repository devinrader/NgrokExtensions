using NgrokExtensions.Tunnels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Services
{
    public interface ITunnelManagerService
    {
        ObservableCollection<Tunnel> Tunnels { get; set; }

        bool IsMonitoring { get; set; }

        Task InitializeTunnelsAsync();
    }

    public interface STunnelManagerService
    {

    }
}
