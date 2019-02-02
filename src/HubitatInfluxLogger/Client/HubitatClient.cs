using InfluxDB.Collector;
using Newtonsoft.Json;
using Serilog;
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
        private readonly ILogger _logger;
        private readonly WebSocket _webSocket;
        private readonly MetricsCollector _collector;

        public HubitatClient(HubitatOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;
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
            _logger.Information("Socket opened");
        }

        private void SocketError(ErrorEventArgs e)
        {
            _logger.Error(e.Exception, "Socket error: {Message}", e.Message);
        }

        private void SocketClosed(CloseEventArgs e)
        {
            _logger.Warning("Socket closed. Clean: {Clean}, Reason: {Reason}, Code: {Code}", e.WasClean, e.Reason, e.Code);
            _webSocket.Connect();
        }

        private void MessageReceived(string message)
        {
            _logger.Information("Message Received: {Message}", message);
            try
            {
                var hubMessage = JsonConvert.DeserializeObject<HubMessage>(message);
                var processedMessage = ProcessMessage(hubMessage);
                _logger.Debug("Writing Data: {Data}", processedMessage.Data);
                _logger.Debug("Writing Tags: {Tags}", processedMessage.Tags);
                _collector.Write(hubMessage.Name, processedMessage.Data, processedMessage.Tags);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error deserializing message");
            }
        }

        public async Task Start()
        {
            _logger.Debug("Starting Hubitat Client");
            _webSocket.Connect();
        }

        public async Task Stop()
        {
            _logger.Debug("Stopping Hubitat Client");
            if (_webSocket.ReadyState == WebSocketState.Open)
            {

                _logger.Debug("Closing WebSocket");
                _webSocket.CloseAsync(CloseStatusCode.Normal);
            }

            Metrics.Close();
        }

        public (Dictionary<string, object> Data, Dictionary<string, string> Tags) ProcessMessage(HubMessage message)
        {
            var tags = new Dictionary<string, string>();
            var data = new Dictionary<string, object>();

            tags.Add("deviceName", message.DisplayName);
            tags.Add("deviceId", message.DeviceId);
            tags.Add("locationId", message.LocationId);
            tags.Add("hubId", message.HubId);
            tags.Add("installedAppId", message.InstalledAppId);
            tags.Add("source", message.Source);


            switch (message.Name)
            {
                case "acceleration":
                    tags.Add("unit", "acceleration");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "alarm":
                    tags.Add("unit", "alarm");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "button":
                    tags.Add("unit", "button");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "pushed" ? 0 : 1);
                    break;
                case "carbonMonoxide":
                    tags.Add("unit", "carbonMonoxide");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "consumableStatus":
                    tags.Add("unit", "consumableStatus");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "good" ? 1 : 0);
                    break;
                case "contact":
                    tags.Add("unit", "contact");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
                case "door":
                    tags.Add("unit", "door");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
                case "lock":
                    tags.Add("unit", "lock");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "locked" ? 1 : 0);
                    break;
                case "motion":
                    tags.Add("unit", "motion");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "mute":
                    tags.Add("unit", "mute");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "muted" ? 1 : 0);
                    break;
                case "presence":
                    tags.Add("unit", "presence");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "present" ? 1 : 0);
                    break;
                case "shock":
                    tags.Add("unit", "shock");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "sleeping":
                    tags.Add("unit", "sleeping");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "sleeping" ? 1 : 0);
                    break;
                case "smoke":
                    tags.Add("unit", "smoke");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "sound":
                    tags.Add("unit", "sound");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "switch":
                    tags.Add("unit", "switch");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "on" ? 1 : 0);
                    break;
                case "tamper":
                    tags.Add("unit", "tamper");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "thermostatMode":
                    tags.Add("unit", "thermostatMode");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "thermostatFanMode":
                    tags.Add("unit", "thermostatFanMode");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "thermostatOperatingState":
                    tags.Add("unit", "thermostatOperatingState");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "heating" ? 1: 0);
                    break;
                case "thermostatSetpointMode":
                    tags.Add("unit", "thermostatSetpointMode");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "followSchedule" ? 0 : 1);
                    break;
                case "threeAxis":
                    tags.Add("unit", "threeAxis");
                    var valueXYZ = message.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);
                    data.Add("valueX", $"{message.Value[0]}i");
                    data.Add("valueY", $"{message.Value[1]}i");
                    data.Add("valueZ", $"{message.Value[2]}i");
                    break;
                case "touch":
                    tags.Add("unit", "touch");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "touched" ? 1 : 0);
                    break;
                case "optimisation":
                    tags.Add("unit", "optimisation");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "windowFunction":
                    tags.Add("unit", "windowFunction");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "water":
                    tags.Add("unit", "water");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "wet" ? 1 : 0);
                    break;
                case "windowShade":
                    tags.Add("unit", "windowShade");
                    data.Add("value", message.Value);
                    data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
                default:
                    tags.Add("unit", message.Unit);
                    data.Add("value", message.Value);
                    break;
            }

            return (data, tags);
        }
    }
}
