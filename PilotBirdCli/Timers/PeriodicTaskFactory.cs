﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PilotBirdCli.Tasks
{
    internal class PeriodicTaskFactory
    {
        /// <summary>
        ///     Starts the periodic task.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="intervalInMilliseconds">The interval in milliseconds.</param>
        /// <param name="delayInMilliseconds">The delay in milliseconds, i.e. how long it waits to kick off the timer.</param>
        /// <param name="duration">The maximum time this task is allowed to run.</param>
        /// <param name="maxIterations">The max iterations.</param>
        /// <param name="synchronous">
        ///     Executes each period in a blocking fashion and each execution of the task contributes to the
        ///     total duration of the Task.
        /// </param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="periodicTaskCreationOptions">Used to create the task for executing the Action</param>
        /// <returns>Task</returns>
        /// <remarks>
        ///     Exceptions that occur in the <paramref name="action" /> need to be handled in the action itself. These exceptions
        ///     will not be bubbled up to the periodic task.
        /// </remarks>
        public static Task Start(Action action,
            int intervalInMilliseconds = Timeout.Infinite,
            int delayInMilliseconds = 0,
            int duration = Timeout.Infinite,
            int maxIterations = -1,
            bool synchronous = false,
            CancellationToken cancelToken = new CancellationToken(),
            TaskCreationOptions periodicTaskCreationOptions = TaskCreationOptions.None)
        {
            var stopWatch = new Stopwatch();
            Action wrapperAction = () =>
            {
                CheckIfCancelled(cancelToken);
                action();
            };

            Action mainAction = () =>
            {
                MainPeriodicTaskAction(intervalInMilliseconds, delayInMilliseconds, duration, maxIterations,
                    cancelToken, stopWatch, synchronous, wrapperAction, periodicTaskCreationOptions);
            };

            return Task.Factory.StartNew(mainAction, cancelToken, TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
        }

        /// <summary>
        ///     Mains the periodic task action.
        /// </summary>
        /// <param name="intervalInMilliseconds">The interval in milliseconds.</param>
        /// <param name="delayInMilliseconds">The delay in milliseconds.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxIterations">The max iterations.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="stopWatch">The stop watch.</param>
        /// <param name="synchronous">
        ///     Executes each period in a blocking fashion and each execution of the task contributes to the
        ///     total duration of the Task.
        /// </param>
        /// <param name="wrapperAction">The wrapper action.</param>
        /// <param name="periodicTaskCreationOptions">
        ///     <see cref="TaskCreationOptions" /> used to create a sub task for executing
        ///     the <see cref="Action" />.
        /// </param>
        private static void MainPeriodicTaskAction(int intervalInMilliseconds,
            int delayInMilliseconds,
            int duration,
            int maxIterations,
            CancellationToken cancelToken,
            Stopwatch stopWatch,
            bool synchronous,
            Action wrapperAction,
            TaskCreationOptions periodicTaskCreationOptions)
        {
            var subTaskCreationOptions = TaskCreationOptions.AttachedToParent | periodicTaskCreationOptions;

            CheckIfCancelled(cancelToken);

            if (delayInMilliseconds > 0) Thread.Sleep(delayInMilliseconds);

            if (maxIterations == 0) return;

            var iteration = 0;

            // Using a ManualResetEventSlim as it is more efficient in small intervals.
            // In the case where longer intervals are used, it will automatically use a standard WaitHandle
            // Refer to http://msdn.microsoft.com/en-us/library/vstudio/5hbefs30(v=vs.100).aspx

            using (var periodResetEvent = new ManualResetEventSlim(false))
            {
                // Main periodic logic. Basically loop through this block executing the action
                while (true)
                {
                    CheckIfCancelled(cancelToken);

                    var subTask = Task.Factory.StartNew(wrapperAction, cancelToken, subTaskCreationOptions,
                        TaskScheduler.Current);

                    if (synchronous)
                    {
                        stopWatch.Start();
                        try
                        {
                            subTask.Wait(cancelToken);
                        }
                        catch
                        {
                            /* do not let an errant subtask to kill the periodic task...*/
                        }

                        stopWatch.Stop();
                    }

                    // use the same Timeout setting as the System.Threading.Timer, infinite timeout will execute only one iteration.
                    if (intervalInMilliseconds == Timeout.Infinite) break;

                    iteration++;

                    if (maxIterations > 0 && iteration >= maxIterations) break;

                    try
                    {
                        stopWatch.Start();
                        periodResetEvent.Wait(intervalInMilliseconds, cancelToken);
                        stopWatch.Stop();
                    }
                    finally
                    {
                        periodResetEvent.Reset();
                    }

                    CheckIfCancelled(cancelToken);

                    if (duration > 0 && stopWatch.ElapsedMilliseconds >= duration) break;
                }
            }
        }

        /// <summary>
        ///     Checks if cancelled.
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        private static void CheckIfCancelled(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException("cancellationToken");

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}