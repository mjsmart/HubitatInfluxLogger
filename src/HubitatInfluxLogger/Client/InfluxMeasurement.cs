using System;
using System.Collections.Generic;

namespace HubitatInfluxLogger.Client
{
    public class InfluxMeasurement
    {
        public InfluxMeasurement()
        {
            LoggedAt = DateTime.Now.ToUniversalTime();
        }

        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        public DateTime LoggedAt { get; set; }

        public Dictionary<string, string> Tags => _tags;

        public Dictionary<string, object> Data => _data;

        public string Name { get; set; }
    }
}