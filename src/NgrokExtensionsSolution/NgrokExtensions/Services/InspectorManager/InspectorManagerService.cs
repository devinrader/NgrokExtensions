using DrWPF.Windows.Data;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NgrokExtensions.TrafficInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace NgrokExtensions.Services
{
    public class InspectorManagerService : IInspectorManagerService, SInspectorManagerService
    {
        private IAsyncServiceProvider serviceProvider;
        private ILoggerService loggerService;

        private ClientWebSocket webSocket = null;

        private Uri uri = new Uri("ws://localhost:4040/_ws");
        private const int receiveChunkSize = 1024;
        private const bool verbose = true;

        private static ObservableDictionary<string, RoundTrip> roundtrips = new ObservableDictionary<string, RoundTrip>();

        public InspectorManagerService(IAsyncServiceProvider provider)
        {
            serviceProvider = provider;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            loggerService = await serviceProvider.GetServiceAsync(typeof(SLoggerService)) as ILoggerService;
            //webApplicationManager = await serviceProvider.GetServiceAsync(typeof(SWebApplicationsManagerService)) as IWebApplicationsManagerService;
        }

        public async Task StartInspectorAsync()
        {
            try
            {
                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(uri, CancellationToken.None);
                await System.Threading.Tasks.Task.WhenAll(ReceiveAsync(webSocket));
            }
            catch (Exception ex)
            {
                await loggerService.WriteLineToOutputWindowAsync($"Exception: {ex}");
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();

                await loggerService.WriteLineToOutputWindowAsync("WebSocket closed.");
            }

        }

        private static async System.Threading.Tasks.Task ReceiveAsync(ClientWebSocket webSocket)
        {
            ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[receiveChunkSize]);

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = null;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        }
                        else
                        {
                            await ms.WriteAsync(buffer.Array, buffer.Offset, result.Count);
                        }

                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            var content = await reader.ReadToEndAsync();
                            var type = JObject.Parse(content).SelectToken("$.Type").Value<string>();

                            if (type == "RoundTrip")
                            {
                                var rt = GetFirstInstance<RoundTrip>("Payload", content);

                                if (roundtrips.ContainsKey(rt.EntryId))
                                {
                                    roundtrips[rt.EntryId] = rt;
                                }
                                else
                                {
                                    roundtrips.Add(rt.EntryId, rt);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static T GetFirstInstance<T>(string propertyName, string json)
        {
            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName
                        && (string)jsonReader.Value == propertyName)
                    {
                        jsonReader.Read();

                        var serializer = new JsonSerializer();
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }
                return default(T);
            }
        }

    }
}
