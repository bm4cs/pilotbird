using System;
using System.Threading;
using PilotBirdCli.CacheMan;
using PilotBirdCli.Timer;

namespace PilotBirdCli
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var cancelSource = new CancellationTokenSource();
            new Thread(() => CancelOnInput(cancelSource)).Start();

            var cacheManager = new CacheManager();
            cacheManager.StartAsync(cancelSource).Wait();
            
            // wait() only for console app demo purposes.
            //var taskTimers = new TaskTimers();
            //taskTimers.StartAsync(cancelSource).Wait();
        }

        private static void CancelOnInput(CancellationTokenSource cancelSource)
        {
            Console.ReadKey();
            cancelSource.Cancel();
        }
    }
}
