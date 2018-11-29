//using System.Collections.Generic;
//using System.Net;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using Newtonsoft.Json;
//using NgrokExtensions.Tunnels;
//using RichardSzalay.MockHttp;

//namespace NgrokExtensions.Test
//{
//    [TestClass]
//    public class NgrokUtilsTest
//    {
//        private MockHttpMessageHandler _mockHttp;
//        private HttpClient _client;
//        private Mock<IErrorDisplayFunc> _mockErrorDisplay;
//        private FakeNgrokProcess _ngrokProcess;
//        //private NgrokUtils _utils;
//        private Dictionary<string, WebApplicationConfig> _webApps;
//        private ApiRequest _expectedRequest;
//        private ApiResponse _emptyTunnelsResponse;

//        [TestInitialize]
//        public void Initialize()
//        {
//            _mockHttp = new MockHttpMessageHandler();
//            _client = new HttpClient(_mockHttp);

//            _webApps = new Dictionary<string, WebApplicationConfig>
//            {
//                {
//                    "fakeApp",
//                    new WebApplicationConfig
//                    {
//                        PortNumber = 1234,
//                        SubDomain = "fake-app"
//                    }
//                }
//            };

//            _mockErrorDisplay = new Mock<IErrorDisplayFunc>();
//            _mockErrorDisplay.Setup(x => x.ShowError(It.IsAny<string>()))
//                .Returns(Task.FromResult(0))
//                .Verifiable("Error display not called.");

//            _ngrokProcess = new FakeNgrokProcess("");
//            _utils = new NgrokUtils(_webApps, "", _mockErrorDisplay.Object.ShowError, _client, _ngrokProcess);

//            _emptyTunnelsResponse = new ApiResponse
//            {
//                tunnels = new Tunnel[0],
//                uri = ""
//            };

//            _expectedRequest = new ApiRequest
//            {
//                addr = "localhost:1234",
//                host_header = "localhost:1234",
//                name = "fakeApp",
//                proto = "http",
//                subdomain = "fake-app"
//            };
//        }

//        [TestCleanup]
//        public void Cleanup()
//        {
//            Assert.AreEqual(0, _ngrokProcess.StartCount);
//            _mockHttp.VerifyNoOutstandingExpectation();
//        }

//        [TestMethod]
//        public async Task TestStartTunnel()
//        {
//            _mockHttp.Expect("http://localhost:4040/api/tunnels")
//                .Respond("application/json", JsonConvert.SerializeObject(_emptyTunnelsResponse));

//            _mockHttp.Expect(HttpMethod.Post, "http://localhost:4040/api/tunnels")
//                .WithContent(JsonConvert.SerializeObject(_expectedRequest))
//                .Respond("application/json", "{}");

//            await _utils.StartTunnelsAsync();
//        }

//        [TestMethod]
//        public async Task TestStartTunnelNotRunning()
//        {
//            _mockHttp.Expect("http://localhost:4040/api/tunnels")
//                .Respond(HttpStatusCode.BadGateway);

//            _mockHttp.Expect("http://localhost:4040/api/tunnels")
//                .Respond("application/json", JsonConvert.SerializeObject(_emptyTunnelsResponse));

//            _mockHttp.Expect(HttpMethod.Post, "http://localhost:4040/api/tunnels")
//                .WithContent(JsonConvert.SerializeObject(_expectedRequest))
//                .Respond("application/json", "{}");

//            await _utils.StartTunnelsAsync();

//            Assert.AreEqual(1, _ngrokProcess.StartCount);
//            _ngrokProcess.StartCount = 0;
//        }

//        [TestMethod]
//        public async Task TestStartTunnelExisting()
//        {
//            var tunnels = new ApiResponse
//            {
//                tunnels = new []
//                {
//                    new Tunnel
//                    {
//                        config = new Config
//                        {
//                            addr = "localhost:1234",
//                            inspect = true
//                        },
//                        name = "fakeApp",
//                        proto = "https",
//                        public_url = "https://fake-app.ngrok.io"
//                    },
//                    new Tunnel
//                    {
//                        config = new Config
//                        {
//                            addr = "localhost:1234",
//                            inspect = true
//                        },
//                        name = "fakeApp",
//                        proto = "http",
//                        public_url = "http://fake-app.ngrok.io"
//                    }
//                },
//                uri = ""
//            };

//            _mockHttp.Expect("http://localhost:4040/api/tunnels")
//                .Respond("application/json", JsonConvert.SerializeObject(tunnels));

//            await _utils.StartTunnelsAsync();
//        }

//        [TestMethod]
//        public void TestNgrokIsInstalled()
//        {
//            Assert.AreEqual(_ngrokProcess.IsInstalled(), _utils.NgrokIsInstalled());
//        }
//    }
//}
