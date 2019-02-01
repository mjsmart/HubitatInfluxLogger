using System;
using System.Collections.Generic;
using System.Text;

namespace HubitatInfluxLogger.Client
{
    public class HubitatOptions
    {
        public string WebSocketURL { get; set; }

        public string InfluxDbURL { get; set; }

        public string InfluxDbDatabase { get; set; }
        public string InfluxDbUsername { get; set; }
        public string InfluxDbPassword { get; set; }

        public int BatchInterval { get; set; }
    }
}
