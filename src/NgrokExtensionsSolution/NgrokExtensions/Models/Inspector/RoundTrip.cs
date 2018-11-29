using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.TrafficInspector
{
    public class RoundTrip
    {
        public string EntryId { get; set; }
        public int Duration { get; set; }
        public int Start { get; set; }
        public string RemoteAddr { get; set; }
        public Request Request { get; set; }
        public Response Response { get; set; }
    }
}
