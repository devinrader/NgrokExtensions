using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using NgrokExtensions.Models;
using NgrokExtensions.Tunnels;

namespace NgrokExtensions.Services
{

    public class RepositoryService : SRepositoryService, IRepositoryService
    {
        private IAsyncServiceProvider serviceProvider;

        private IList<Tunnel> _tunnels;
        private IList<WebApplication> _webApplications;

        public RepositoryService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;
        }

        public IList<Tunnel> Tunnels
        {
            get
            {
                if (_tunnels == null)
                {
                    _tunnels = new List<Tunnel>();
                }
                return _tunnels;
            }
            set
            {
                _tunnels = value;
            }
        }

        public IList<WebApplication> WebApplications
        {
            get
            {
                if (_webApplications == null)
                {
                    _webApplications = new List<WebApplication>();
                }
                return _webApplications;
            }
            set
            {
                _webApplications = value;
            }
        }
    }
}
