using System;
using System.Threading.Tasks;

namespace HubitatInfluxLogger.Client
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }
    }
}
