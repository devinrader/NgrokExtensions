using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.Services
{
    [Guid("a7c89067-05d4-4146-bbde-fd9a7a287c8f")]
    [ComVisible(true)]
    public interface ILoggerService
    {
        
        Task WriteToOutputWindowAsync(string message);
        Task WriteLineToOutputWindowAsync(string message);
        //void Write(string message, Importance importance);
    }

    [Guid("06ab379f-a2d6-4937-8485-ff6a758323fa")]
    public interface SLoggerService
    {

    }
}
