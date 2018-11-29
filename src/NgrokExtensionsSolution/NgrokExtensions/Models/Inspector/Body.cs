using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NgrokExtensions.TrafficInspector
{
    public class Body
    {
        public bool Binary {get;set;}
        public string RawContentType{get;set;}
        public string Encoding{get;set;}
        public string ContentType{get;set;}
        public string Text{get;set;}
        public string Error{get;set;}
        public int ErrorOffset{get;set;}
        public string Form{get;set;}
        public int  Size{get;set;}
        public int CapturedSize{get;set;}
        public int DecodedSize{get;set;}
        public int DisplaySize{get;set;}
    }
}
