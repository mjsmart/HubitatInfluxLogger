using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace HubitatInfluxLogger.Client
{
    class Program
    {
        private const string APPLICATION_SETTINGS_FILE_NAME = "appsettings.json";
        private static HubitatClient _hubitat;

        static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile(APPLICATION_SETTINGS_FILE_NAME, optional: false, reloadOnChange: true)
                   .AddUserSecrets(typeof(Program).GetTypeInfo().Assembly)
                   .Build();


            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Information("Starting Hubitat Influx Logger");

            var hubitatOptions = new HubitatOptions();
            configuration.Bind("Hubitat", hubitatOptions);

            Log.Information("Configured to receive data from {WebSocketURL}", hubitatOptions.WebSocketURL);
            Log.Information("Configured to write data to {InfluxDbDatabase} at {InfluxDbURL}", hubitatOptions.InfluxDbDatabase,  hubitatOptions.InfluxDbURL);

            _hubitat = new HubitatClient(hubitatOptions, Log.ForContext<HubitatClient>());
            await _hubitat.Start();
            Log.Information("Started Hubitat Client");


            using (var shutdownCts = new CancellationTokenSource())
            {
                try
                {
                    var myTask = Task.Factory.StartNew(() => Worker(shutdownCts.Token), shutdownCts.Token);
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        Shutdown(shutdownCts);
                        eventArgs.Cancel = true;
                    };

                    AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => Shutdown(shutdownCts);

                    await _hubitat.Start();
                    Log.Information("Started Hubitat Client");
                    Console.WriteLine("Application is running. Press Ctrl+C to shut down.");

                    while (!shutdownCts.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }

                    Log.Information("Application is shutting down...");
                    await _hubitat.Stop();
                }
                catch(Exception ex)
                {
                    Log.Logger.Error(ex, "Error whilst shutting down");
                }
            }
        }


        private static void Worker(CancellationToken cToken)
        {
            while (!cToken.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }

            Log.Debug("Cancelling worker thread");
        }

        private static void Shutdown(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
