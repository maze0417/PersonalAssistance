using System;
using System.Threading;
using System.Threading.Tasks;

namespace PunchCardApp
{
    public static class AsyncHelper
    {
        private static readonly TaskFactory MyTaskFactory = new
            TaskFactory(CancellationToken.None,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return MyTaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            MyTaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunAsync(Func<Task> func)
        {
            MyTaskFactory
                .StartNew(func)
                .Unwrap();
        }

        public static void RunAsync(Action action)
        {
            Task.Run(action);
        }

        public static void RunAsync(Func<object, Task> func, object arg0)
        {
            MyTaskFactory
                .StartNew(func, arg0)
                .Unwrap();
        }

        public static CancellationTokenSource RunLongRunningAsync(Func<CancellationToken, Task> func)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            MyTaskFactory
                .StartNew(() =>
                        func(cancellationTokenSource.Token),
                    cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default)
                .Unwrap();
            return cancellationTokenSource;
        }
    }
}