using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HubitatInfluxLogger.Client
{
    class Program
    {
        private const string APPLICATION_SETTINGS_FILE_NAME = "appsettings.json";
        private static HubitatClient _hubitat;

        public static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory()) // Directory where the json files are located
                   .AddJsonFile(APPLICATION_SETTINGS_FILE_NAME, optional: false, reloadOnChange: true)
                   .Build();

            var hubitatOptions = new HubitatOptions();
            configuration.Bind("Hubitat", hubitatOptions);

            _hubitat = new HubitatClient(hubitatOptions);
            await _hubitat.Start();

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
            }

            await _hubitat.Stop();
        }
    }
}
