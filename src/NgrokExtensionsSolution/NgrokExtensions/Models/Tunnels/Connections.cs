using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class Connections
    {
        public int count { get; set; }
        public int gauge { get; set; }
        public decimal rate1 { get; set; }
        public decimal rate5 { get; set; }
        public decimal rate15 { get; set; }
        public decimal p50 { get; set; }
        public decimal p90 { get; set; }
        public decimal p95 { get; set; }
        public decimal p99 { get; set; }
    }
}
