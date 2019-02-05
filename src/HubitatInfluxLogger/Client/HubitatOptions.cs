using System.Collections.Generic;

namespace HubitatInfluxLogger.Client
{
    public class HubitatOptions
    {
        public HubitatOptions()
        {
            DevicesToLog = new List<string>();
            DevicesToIgnore = new List<string>();
            MeasurementsToLog = new List<string>();
            MeasurementsToIgnore = new List<string>();
        }

        public string WebSocketURL { get; set; }

        public string InfluxDbURL { get; set; }

        public string InfluxDbDatabase { get; set; }
        public string InfluxDbUsername { get; set; }
        public string InfluxDbPassword { get; set; }

        public int BatchInterval { get; set; }

        public List<string> DevicesToLog { get; set; }
        public List<string> DevicesToIgnore { get; set; }

        public List<string> MeasurementsToLog { get; set; }

        public List<string> MeasurementsToIgnore { get; set; }
    }
}