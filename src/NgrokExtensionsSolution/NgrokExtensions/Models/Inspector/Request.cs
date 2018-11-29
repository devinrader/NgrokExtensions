using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.TrafficInspector
{
    public class Request
    {
        public string Raw {get;set;}
        public string MethodPath {get;set;}
        //public Params Params {get;set;}
        public Dictionary<string,string[]> Header {get;set;}
        public Body Body {get;set;}
        public int Size {get;set;}
        public int CapturedSize {get;set;}
    }
}
