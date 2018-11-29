using NgrokExtensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Services
{
    public interface IWebApplicationsManagerService
    {
        IDictionary<string, WebApplication> Projects { get; set; }
    }

    public interface SWebApplicationsManagerService
    {
    }
}
