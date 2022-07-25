using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PilotBirdCli.Tasks
{
    class Worker
    {
        public int Id;
        public int SleepTimeout;

        public async Task DoWork(DateTime testStart)
        {
            var workerStart = DateTime.Now;

            Console.WriteLine("Worker {0} started on thread {1}, beginning {2:F2} seconds after test start.",
                Id, Thread.CurrentThread.ManagedThreadId, (workerStart - testStart).TotalSeconds);

            await Task.Run(() => Thread.Sleep(SleepTimeout));

            var workerEnd = DateTime.Now;

            Console.WriteLine("Worker {0} stopped; the worker took {1:F2} seconds, and it finished {2:F2} seconds after the test start.",
                Id, (workerEnd - workerStart).TotalSeconds, (workerEnd - testStart).TotalSeconds);
        }
    }
}
