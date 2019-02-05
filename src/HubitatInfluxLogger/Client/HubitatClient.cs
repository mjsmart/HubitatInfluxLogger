using InfluxDB.Collector;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PureWebSockets;
using System.Net.WebSockets;
using InfluxDB.Collector.Diagnostics;
using System.Text.RegularExpressions;

namespace HubitatInfluxLogger.Client
{
    public class HubitatClient
    {
        private readonly HubitatOptions _options;
        private readonly ILogger _logger;
        private readonly PureWebSocket _webSocket;
        private readonly MetricsCollector _collector;

        public HubitatClient(HubitatOptions options, ILogger logger)
        {
            _options = options;
            _logger = logger;

            var socketOptions = new PureWebSocketOptions()
            {
                DebugMode = true,
                SendDelay = 100,
                IgnoreCertErrors = true,
                MyReconnectStrategy = new ReconnectStrategy(2000, 4000, 20)
            };

            _webSocket = new PureWebSocket(_options.WebSocketURL, socketOptions);
            _webSocket.OnOpened += () => SocketOpen();
            _webSocket.OnStateChanged += (newState, previousState) => SocketStateChanged(newState, previousState);
            _webSocket.OnMessage += (message) => MessageReceived(message);
            _webSocket.OnClosed += (reason) => SocketClosed(reason);
            _webSocket.OnSendFailed += (data, ex) => SocketSendFailed(data, ex);
            
            _webSocket.OnError += (e) => SocketError(e);

            _collector = Metrics.Collector = new CollectorConfiguration()
                .Batch.AtInterval(TimeSpan.FromSeconds(options.BatchInterval))
                .WriteTo.InfluxDB(options.InfluxDbURL, options.InfluxDbDatabase, options.InfluxDbUsername, options.InfluxDbPassword)
                .CreateCollector();

            CollectorLog.RegisterErrorHandler((string message, Exception ex) =>
            {
                _logger.Error(ex, "Failed to write metrics to InfluxDB: {Message}", message);
            });
        }

        private void SocketSendFailed(string data, Exception ex)
        {
            _logger.Error(ex, "Failed to send message {Data}", data);
        }

        private void SocketStateChanged(WebSocketState newState, WebSocketState prevState)
        {
            _logger.Information("Socket state changed from {PreviousState} to {NewState}", prevState, newState);
        }

        private void SocketOpen()
        {
            _logger.Information("Socket opened");
        }

        private void SocketError(Exception e)
        {
            _logger.Error(e, "Socket error: {Message}", e.Message);
        }

        private void SocketClosed(WebSocketCloseStatus  reason)
        {
            _logger.Warning("Socket closed. Reason: {Reason}", reason);
            _webSocket.Connect();
        }

        private void MessageReceived(string message)
        {
            _logger.Information("Message Received: {Message}", message);
            try
            {
                var hubMessage = JsonConvert.DeserializeObject<HubMessage>(message);
                var processedMessage = ProcessMessage(hubMessage);

                if (MessageShouldBeLogged(processedMessage))
                {
                    _logger.Debug("Writing Data: {Data}", processedMessage.Data);
                    _logger.Debug("Writing Tags: {Tags}", processedMessage.Tags);
                    _collector.Write(hubMessage.Name, processedMessage.Data, processedMessage.Tags, processedMessage.LoggedAt);
                }
                else
                {
                    _logger.Information("Ignoring measurement");
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error deserializing message");
            }
        }

        private bool MessageShouldBeLogged(InfluxMeasurement measurement) {
            bool messageShouldBeLogged = true;
            var deviceId = measurement.Tags["deviceId"];
            var measurementName = measurement.Name;

            if(_options.DevicesToLog.Count > 0 && !_options.DevicesToLog.Contains(deviceId))
            {
                messageShouldBeLogged = false;
            }

            if (_options.DevicesToIgnore.Contains(deviceId))
            {
                messageShouldBeLogged = false;
            }

            if (_options.MeasurementsToLog.Count > 0 && !_options.MeasurementsToLog.Contains(measurementName))
            {
                messageShouldBeLogged = false;
            }

            if (_options.MeasurementsToIgnore.Contains(measurementName))
            {
                messageShouldBeLogged = false;
            }

            return messageShouldBeLogged;
        }

        public async Task Start()
        {
            _logger.Debug("Starting Hubitat Client");

            try
            {
                await _webSocket.ConnectAsync();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error starting WebSocket client");
                throw;
            }
        } 

        public async Task Stop()
        {
            _logger.Debug("Stopping Hubitat Client");
            if (_webSocket.State != WebSocketState.Closed)
            {

                _logger.Debug("Closing WebSocket");
                _webSocket.Disconnect();
            }

            Metrics.Close();
        }

        public InfluxMeasurement ProcessMessage(HubMessage message)
        {
            var measurement = new InfluxMeasurement();
            measurement.Name = message.Name;

            measurement.Tags.Add("deviceName", message.DisplayName);
            measurement.Tags.Add("deviceId", message.DeviceId);
            measurement.Tags.Add("locationId", message.LocationId);
            measurement.Tags.Add("hubId", message.HubId);
            measurement.Tags.Add("installedAppId", message.InstalledAppId);
            measurement.Tags.Add("source", message.Source);


            switch (message.Name)
            {
                case "acceleration":
                    measurement.Tags.Add("unit", "acceleration");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "alarm":
                    measurement.Tags.Add("unit", "alarm");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "button":
                    measurement.Tags.Add("unit", "button");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "pushed" ? 0 : 1);
                    break;
                case "carbonMonoxide":
                    measurement.Tags.Add("unit", "carbonMonoxide");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "consumableStatus":
                    measurement.Tags.Add("unit", "consumableStatus");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "good" ? 1 : 0);
                    break;
                case "contact":
                    measurement.Tags.Add("unit", "contact");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
                case "door":
                    measurement.Tags.Add("unit", "door");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
                case "lastCheckin":
                    measurement.Tags.Add("unit", "ticks");
                    measurement.Data.Add("value", message.Value);
                    break;
                case "lastInactive":
                    measurement.Tags.Add("unit", "ticks");
                    measurement.Data.Add("value", message.Value);
                    break;
                case "lastMotion":
                    measurement.Tags.Add("unit", "ticks");
                    measurement.Data.Add("value", message.Value);
                    break;
                case "lock":
                    measurement.Tags.Add("unit", "lock");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "locked" ? 1 : 0);
                    break;
                case "motion":
                    measurement.Tags.Add("unit", "motion");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "mute":
                    measurement.Tags.Add("unit", "mute");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "muted" ? 1 : 0);
                    break;
                case "presence":
                    measurement.Tags.Add("unit", "presence");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "present" ? 1 : 0);
                    break;
                case "shock":
                    measurement.Tags.Add("unit", "shock");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "sleeping":
                    measurement.Tags.Add("unit", "sleeping");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "sleeping" ? 1 : 0);
                    break;
                case "smoke":
                    measurement.Tags.Add("unit", "smoke");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "sound":
                    measurement.Tags.Add("unit", "sound");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "switch":
                    measurement.Tags.Add("unit", "switch");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "on" ? 1 : 0);
                    break;
                case "tamper":
                    measurement.Tags.Add("unit", "tamper");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "detected" ? 1 : 0);
                    break;
                case "thermostatMode":
                    measurement.Tags.Add("unit", "thermostatMode");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "thermostatFanMode":
                    measurement.Tags.Add("unit", "thermostatFanMode");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "off" ? 0 : 1);
                    break;
                case "thermostatOperatingState":
                    measurement.Tags.Add("unit", "thermostatOperatingState");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "heating" ? 1: 0);
                    break;
                case "thermostatSetpointMode":
                    measurement.Tags.Add("unit", "thermostatSetpointMode");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "followSchedule" ? 0 : 1);
                    break;
                case "threeAxis":
                    measurement.Tags.Add("unit", "threeAxis");
                    var valueXYZ = message.Value.Split(",", StringSplitOptions.RemoveEmptyEntries);

                    if (int.TryParse(valueXYZ[0], out int x))
                    {
                        measurement.Data.Add("valueX", x);
                    }


                    if (int.TryParse(valueXYZ[1], out int y))
                    {
                        measurement.Data.Add("valueY", y);
                    }


                    if (int.TryParse(valueXYZ[2], out int z))
                    {
                        measurement.Data.Add("valueZ", z);
                    }
                    break;
                case "touch":
                    measurement.Tags.Add("unit", "touch");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "touched" ? 1 : 0);
                    break;
                case "optimisation":
                    measurement.Tags.Add("unit", "optimisation");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "windowFunction":
                    measurement.Tags.Add("unit", "windowFunction");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "active" ? 1 : 0);
                    break;
                case "water":
                    measurement.Tags.Add("unit", "water");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "wet" ? 1 : 0);
                    break;
                case "windowShade":
                    measurement.Tags.Add("unit", "windowShade");
                    measurement.Data.Add("value", message.Value);
                    measurement.Data.Add("valueBinary", message.Value == "closed" ? 1 : 0);
                    break;
            }

            if(measurement.Data.Count == 0)
            {
                if(Regex.IsMatch(message.Value, @"/.*[^0-9\.,-].*/"))
                {
                    measurement.Data.Add("value", message.Value);
                }
                else
                {
                    measurement.Tags.Add("unit", message.Unit);

                    if(float.TryParse(message.Value, out float floatVal))
                    {
                        measurement.Data.Add("value", floatVal);
                    }
                    else
                    {
                        measurement.Data.Add("value", message.Value);
                    }
                }
            }

            return measurement;
        }
    }
}
