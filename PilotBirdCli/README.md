A collection of .NET Framework 4.x proof of concepts

## Task

Task based programming.

- dispatch patterns\
- Task Parallel Library (TPL)
- `async` and `await` (TAP)
- error handling

Great background reading:

- [Stephen Cleary Async and Await Primer](https://blog.stephencleary.com/2012/02/async-and-await.html)
- [Stackoverflow: Understanding async / await in C#](https://stackoverflow.com/questions/14177891/understanding-async-await-in-c-sharp) great answer by Stephen Cleary
- [Microsoft Docs: Data Structures for Parallel Programming](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/data-structures-for-parallel-programming)

### async/await

- By default an awaitable will capture the current SynchronizationContext, later applying it to the remainder of the async method.
- This is convenient for UI based event handlers, but most of the time it is not.
- Most of the time you don't need to sync back to the main context, and most async methods are designed with composition in mind
- That is, they await other operations, and each represents an async operation itself.
- In this case, tell the awaiter to not capture the current context by calling `ConfigureAwait(false)`
- Example: `var c = await DownloadFileContentsAsync(fn).ConfigureAwait(false);`


## Thread

Managed threads and synchronisation mechanisms.


## Timer

Task based approaches to time interval triggered workloads.

Common use-cases for period triggered work [kudos](https://stackoverflow.com/a/30254440/804423):

1. Don't even run the timer while the task is running.
2. Run the timer (this and the remaining options I'm presenting here all assume the timer continues to run during the execution of the task), but if the task takes longer than the timer period, run the task again immediately after it's completed from the previous timer tick.
3. Only ever initiate execution of the task on a timer tick. If the task takes longer than the timer period, don't start a new task while the current one is executed, and even once the current one has completed, don't start a new one until the next timer tick.
4. If the task takes longer than the timer interval, not only run the task again immediately after it's completed, but run it as many times as necessary until the task has "caught up". I.e. over time, make a best effort to execute the task once for every timer tick.