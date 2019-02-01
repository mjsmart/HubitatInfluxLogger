using InfluxDB.Collector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace HubitatInfluxLogger.Client
{
    public class HubitatClient
    {
        private readonly HubitatOptions _options;
        private readonly WebSocket _webSocket;
        private readonly MetricsCollector _collector;

        public HubitatClient(HubitatOptions options)
        {
            _options = options;
            _webSocket = new WebSocket(_options.WebSocketURL);
            _webSocket.OnMessage += (sender, e) => MessageReceived(e.Data);
            _webSocket.OnClose += (sender, e) => SocketClosed(e);
            _webSocket.OnError += (sender, e) => SocketError(e);
            _webSocket.OnOpen += (sender, e) => SocketOpen(e);

            _collector = Metrics.Collector = new CollectorConfiguration()
                .Batch.AtInterval(TimeSpan.FromSeconds(options.BatchInterval))
                .WriteTo.InfluxDB(options.InfluxDbURL, options.InfluxDbDatabase, options.InfluxDbUsername, options.InfluxDbPassword)
                .CreateCollector();           
        }

        private void SocketOpen(EventArgs e)
        {
            Console.WriteLine($"Socket Open");
        }

        private void SocketError(ErrorEventArgs e)
        {
            Console.WriteLine($"Socket Error: {e.Message}");
        }

        private void SocketClosed(CloseEventArgs e)
        {
            Console.WriteLine($"Socket Closed");
            Console.WriteLine($"Reconnecting...");
            _webSocket.Connect();
        }

        private void MessageReceived(string message)
        {
            Console.WriteLine($"Message Received: {message}");
            try
            {
                var hubMessage = JsonConvert.DeserializeObject<HubMessage>(message);
                _collector.Write(hubMessage.Name, new Dictionary<string, object>
                {
                    {"descriptionText", hubMessage.DescriptionText },
                    {"deviceId", hubMessage.DeviceId },
                    {"displayName", hubMessage.DisplayName },
                    {"source", hubMessage.Source },
                    {"value", hubMessage.Value }
                });
            }
            catch(Exception ex)
            {

                Console.WriteLine($"Error Deserializing Message: {ex.Message}");
            }
        }

        public async Task Start()
        {
            Console.WriteLine($"Connecting...");
            _webSocket.Connect();
        }

        public async Task Stop()
        {
            if(_webSocket.ReadyState == WebSocketState.Open)
            {
                Console.WriteLine($"Socket Closing...");
                _webSocket.CloseAsync(CloseStatusCode.Normal);
            }

            Metrics.Close();
        }
    }
}
