using NgrokExtensions.Models;
using NgrokExtensions.Tunnels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Services
{
    [Guid("24e1dff2-6ce7-4a67-9263-29827777219e")]
    [ComVisible(true)]
    public interface IRepositoryService
    {
        IList<Tunnel> Tunnels { get; set; }
        IList<WebApplication> WebApplications { get; set; }
    }

    [Guid("3cf3e7c2-f144-462e-b700-f65ce9735df3")]
    public interface SRepositoryService
    {
    }
}
