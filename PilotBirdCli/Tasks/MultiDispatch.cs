using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PilotBirdCli.Tasks
{
    internal class MultiDispatch
    {
        public void Start(CancellationTokenSource cancelSource)
        {
            var workers = new List<Worker>
            {
                new Worker { Id = 1, SleepTimeout = 1000 },
                new Worker { Id = 2, SleepTimeout = 2000 },
                new Worker { Id = 3, SleepTimeout = 3000 },
                new Worker { Id = 4, SleepTimeout = 4000 },
                new Worker { Id = 5, SleepTimeout = 5000 }
            };

            var startTime = DateTime.Now;
            Console.WriteLine("Starting test: Parallel.ForEach...");
            PerformTest_ParallelForEach(workers, startTime, cancelSource.Token);
            var endTime = DateTime.Now;
            Console.WriteLine("Test finished after {0:F2} seconds.\n", (endTime - startTime).TotalSeconds);

            startTime = DateTime.Now;
            Console.WriteLine("Starting test: Task.WaitAll...");
            PerformTest_TaskWaitAll(workers, startTime, cancelSource.Token);
            endTime = DateTime.Now;
            Console.WriteLine("Test finished after {0:F2} seconds.\n", (endTime - startTime).TotalSeconds);

            startTime = DateTime.Now;
            Console.WriteLine("Starting test: Task.WhenAll...");
            var task = PerformTest_TaskWhenAll(workers, startTime);
            task.Wait(cancelSource.Token);
            endTime = DateTime.Now;
            Console.WriteLine("Test finished after {0:F2} seconds.\n", (endTime - startTime).TotalSeconds);

            Console.ReadKey();
        }

        private static void PerformTest_ParallelForEach(List<Worker> workers, DateTime testStart, CancellationToken cancelToken)
        {
            Parallel.ForEach(workers, worker => worker.DoWork(testStart).Wait());
        }

        private static void PerformTest_TaskWaitAll(List<Worker> workers, DateTime testStart, CancellationToken cancelToken)
        {
            Task.WaitAll(workers.Select(worker => worker.DoWork(testStart)).ToArray(), cancelToken);
        }

        private static Task PerformTest_TaskWhenAll(List<Worker> workers, DateTime testStart)
        {
            return Task.WhenAll(workers.Select(worker => worker.DoWork(testStart)));
        }
    }
}