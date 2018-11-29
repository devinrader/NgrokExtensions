using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class ListResponse
    {
        public Tunnel[] tunnels { get; set; }
        public string uri { get; set; }
    }
}
