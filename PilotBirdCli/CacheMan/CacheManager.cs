using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PilotBirdCli.CacheMan
{
    internal class CacheManager
    {
        private const int timerSeconds = 5, actionMinSeconds = 1, actionMaxSeconds = 7;

        private static readonly Random _rnd = new Random();

        // kudos: Stephen Cleary https://stackoverflow.com/a/14178317/804423
        // kudos: Peter Duniho https://stackoverflow.com/a/30254440/804423
        public async Task StartAsync(CancellationTokenSource cancelSource)
        {
            Console.WriteLine("Press any key to interrupt timer and exit...");
            Console.WriteLine();

            Console.WriteLine($"Starting at {DateTime.Now:HH:mm:ss.f}, timer interval is {timerSeconds} seconds");
            Console.WriteLine();
            Console.WriteLine();

            RunTimer(cancelSource.Token, M1).Wait(); // wait call for demo only
        }

        private static async Task RunTimer(CancellationToken cancelToken, Func<Action, TimeSpan, Task> timerMethod)
        {
            Console.WriteLine("Testing method {0}()", timerMethod.Method.Name);
            Console.WriteLine();

            try
            {
                await timerMethod(() =>
                {
                    cancelToken.ThrowIfCancellationRequested();
                    DummyAction();
                }, TimeSpan.FromSeconds(timerSeconds));
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("Operation cancelled");
            }
        }



        private static void DummyAction()
        {
            var duration = _rnd.Next(actionMinSeconds, actionMaxSeconds + 1);

            Console.WriteLine("dummy action: {0} seconds", duration);
            Console.Write("    start: {0:HH:mm:ss.f}", DateTime.Now);
            Thread.Sleep(TimeSpan.FromSeconds(duration));
            Console.WriteLine(" - end: {0:HH:mm:ss.f}", DateTime.Now);
        }

        private static async Task M1(Action taskAction, TimeSpan timer)
        {
            // Most basic: always wait specified duration between
            // each execution of taskAction
            while (true)
            {
                await Task.Delay(timer);
                await Task.Run(() => taskAction());
            }
        }

        private static async Task M2(Action taskAction, TimeSpan timer)
        {
            // Simple: wait for specified interval, minus the duration of
            // the execution of taskAction. Run taskAction immediately if
            // the previous execution too longer than timer.

            var remainingDelay = timer;

            while (true)
            {
                if (remainingDelay > TimeSpan.Zero) await Task.Delay(remainingDelay);

                var sw = Stopwatch.StartNew();
                await Task.Run(() => taskAction());
                remainingDelay = timer - sw.Elapsed;
            }
        }

        private static async Task M3a(Action taskAction, TimeSpan timer)
        {
            // More complicated: only start action on time intervals that
            // are multiples of the specified timer interval. If execution
            // of taskAction takes longer than the specified timer interval,
            // wait until next multiple.

            // NOTE: this implementation may drift over time relative to the
            // initial start time, as it considers only the time for the executed
            // action and there is a small amount of overhead in the loop. See
            // M3b() for an implementation that always executes on multiples of
            // the interval relative to the original start time.

            var remainingDelay = timer;

            while (true)
            {
                await Task.Delay(remainingDelay);

                var sw = Stopwatch.StartNew();
                await Task.Run(() => taskAction());

                var remainder = sw.Elapsed.Ticks % timer.Ticks;

                remainingDelay = TimeSpan.FromTicks(timer.Ticks - remainder);
            }
        }

        private static async Task M3b(Action taskAction, TimeSpan timer)
        {
            // More complicated: only start action on time intervals that
            // are multiples of the specified timer interval. If execution
            // of taskAction takes longer than the specified timer interval,
            // wait until next multiple.

            // NOTE: this implementation computes the intervals based on the
            // original start time of the loop, and thus will not drift over
            // time (not counting any drift that exists in the computer's clock
            // itself).

            var remainingDelay = timer;
            var swTotal = Stopwatch.StartNew();

            while (true)
            {
                await Task.Delay(remainingDelay);
                await Task.Run(() => taskAction());

                var remainder = swTotal.Elapsed.Ticks % timer.Ticks;

                remainingDelay = TimeSpan.FromTicks(timer.Ticks - remainder);
            }
        }

        private static async Task M4(Action taskAction, TimeSpan timer)
        {
            // More complicated: this implementation is very different from
            // the others, in that while each execution of the task action
            // is serialized, they are effectively queued. In all of the others,
            // if the task is executing when a timer tick would have happened,
            // the execution for that tick is simply ignored. But here, each time
            // the timer would have ticked, the task action will be executed.
            //
            // If the task action takes longer than the timer for an extended
            // period of time, it will repeatedly execute. If and when it
            // "catches up" (which it can do only if it then eventually
            // executes more quickly than the timer period for some number
            // of iterations), it reverts to the "execute on a fixed
            // interval" behavior.

            var nextTick = timer;
            var swTotal = Stopwatch.StartNew();

            while (true)
            {
                var remainingDelay = nextTick - swTotal.Elapsed;

                if (remainingDelay > TimeSpan.Zero) await Task.Delay(remainingDelay);

                await Task.Run(() => taskAction());
                nextTick += timer;
            }
        }
    }
}
