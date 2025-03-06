using Hangfire.Common;
using Hangfire.States;
using Hangfire;
using Polly.Retry;
using Polly;
using System.Reflection;

namespace PollyHangFireDemo.Utils
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RetryWithPollyAndHangfireAttribute : JobFilterAttribute, IElectStateFilter
    {
        private readonly int _maxAttempts;
        private readonly ITimeSpanForRetry _timeSpanForRetry;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public RetryWithPollyAndHangfireAttribute(int maxAttempts = 5)
        {
            _maxAttempts = maxAttempts;
            _timeSpanForRetry = new TimeSpanForRetry();
            _backgroundJobClient = new BackgroundJobClient();
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                var jobId = context.BackgroundJob.Id;
                int retryAttempt = context.GetJobParameter<int>("RetryAttempt");
                string methodName = context.BackgroundJob.Job.Method.Name;
                string className = context.BackgroundJob.Job.Type.FullName;
                var errorNotifier = context.GetJobParameter<Func<Exception, Task>>("ErrorNotifier");

                if (retryAttempt < _maxAttempts)
                {
                    TimeSpan retryDelay = _timeSpanForRetry.GetSleepDuration(retryAttempt + 1);
                    context.SetJobParameter("RetryAttempt", retryAttempt + 1);

                    _backgroundJobClient.Schedule(() =>
                        RetryJob(className, methodName, retryAttempt + 1, errorNotifier), retryDelay);
                }
                else
                {
                    errorNotifier?.Invoke(failedState.Exception);
                }
            }
        }

        public async Task RetryJob(string className, string methodName, int retryAttempt, Func<Exception, Task> errorNotifier)
        {
            var targetType = Type.GetType(className);
            if (targetType == null)
            {
                Console.WriteLine($"ERROR: Could not find type {className}.");
                return;
            }

            var methodInfo = targetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
            {
                Console.WriteLine($"ERROR: Could not find method {methodName} in {className}.");
                return;
            }

            var instance = Activator.CreateInstance(targetType);
            if (instance == null)
            {
                Console.WriteLine($"ERROR: Could not create an instance of {className}.");
                return;
            }

            await RetryWithPollyAsync(
                async () => await (Task)methodInfo.Invoke(instance, null),
                retryAttempt,
                className,
                methodName,
                errorNotifier);
        }

        private async Task RetryWithPollyAsync(
            Func<Task> action,
            int retryAttempt,
            string className,
            string methodName,
            Func<Exception, Task> errorNotifier)
        {
            AsyncRetryPolicy retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: Math.Min(3, _maxAttempts),
                    sleepDurationProvider: attempt => _timeSpanForRetry.GetSleepDuration(attempt),
                    onRetryAsync: async (exception, timeSpan, attempt, context) =>
                    {
                        Console.WriteLine($"Polly Retry {attempt} in {timeSpan.TotalSeconds} sec");
                        if (errorNotifier != null)
                        {
                            await errorNotifier(exception);
                        }
                    });

            try
            {
                await retryPolicy.ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Polly retries exhausted. Scheduling in Hangfire...");
                if (errorNotifier != null)
                {
                    await errorNotifier(ex);
                }
                _backgroundJobClient.Enqueue(() => RetryJob(className, methodName, retryAttempt + 1, errorNotifier));
            }
        }
    }
}
