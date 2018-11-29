//using NgrokExtensions.Tunnels;
//using System.Diagnostics;

//namespace NgrokExtensions.Test
//{
//    public class FakeNgrokProcess : NgrokProcessWrapper
//    {
//        public FakeNgrokProcess(string exePath) : base(exePath)
//        {
//        }

//        public int StartCount { get; set; } = 0;
//        public ProcessStartInfo LastProcessStartInfo { get; set; }

//        protected override void Start(ProcessStartInfo pi)
//        {
//            StartCount++;
//            LastProcessStartInfo = pi;
//        }
//    }
//}