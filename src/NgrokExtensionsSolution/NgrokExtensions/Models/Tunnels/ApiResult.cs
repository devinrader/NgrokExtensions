using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class ApiResult
    {
        public int error_code { get; set; }
        public int status_code { get; set; }
        public string msg { get; set; }
        public Details details { get; set; }
    }
}
