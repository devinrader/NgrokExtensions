using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Tunnels
{
    public class NgrokInstallerResult
    {
        public NgrokInstallerResult()
        {
            this.Success = false;
        }

        public string ExecutablePath { get; set; }
        public bool Success { get; set; }
    }
}
