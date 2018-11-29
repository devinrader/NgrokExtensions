using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class ApiRequest
    {
        public string name { get; set; }
        public string addr { get; set; }
        public string proto { get; set; }
        public string subdomain { get; set; }
        public string host_header { get; set; }
    }
}
