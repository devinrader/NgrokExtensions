using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class Tunnel
    {
        public string name { get; set; }
        public string uri { get; set; }
        public string public_url { get; set; }
        public string proto { get; set; }
        public Config config { get; set; }
        public Metrics metrics { get; set; }
    }
}
