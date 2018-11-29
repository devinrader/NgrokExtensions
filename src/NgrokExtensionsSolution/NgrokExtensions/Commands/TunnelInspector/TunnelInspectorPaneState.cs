using NgrokExtensions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.TunnelInspector
{
    public class TunnelInspectorPaneState
    {
        public EnvDTE80.DTE2 DTE { get; set; }

        public ILoggerService Logger { get; set; }

        public ITunnelManagerService TunnelManager { get; set; }
    }
}
