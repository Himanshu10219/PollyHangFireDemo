using Polly.Retry;
using Polly;
using System.Diagnostics;
using Hangfire.Server;
using Hangfire;
using Polly.Fallback;
using System;

namespace PollyHangFireDemo
{
    public static class RetryPolicies
    {
        public static int RetryCountLimit { get; set; } = 6;

        private static int CurrentRetryAttemp = 0;

        public static async Task ExecuteWithRetryAsync(Func<Task> action, Func<Exception, TimeSpan, int, Task> onRetry, Func<Exception, Task> onFallback, PerformContext performContext = null)
        {
            string jobId = performContext?.BackgroundJob?.Id;

            AsyncRetryPolicy retryPolicy = Policy.Handle<Exception>(ex => ex is FileNotFoundException) // Catch exceptions that are NOT in the list
                .WaitAndRetryAsync(
                    retryCount: RetryCountLimit,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(15),
                    onRetryAsync: async (exception, timeSpan, retryCount, context) =>
                    {
                        CurrentRetryAttemp = retryCount;
                        if (performContext != null)
                        {
                            performContext.SetJobParameter("Status", "On Retry");
                            performContext.SetJobParameter("Retry Attempt", retryCount);
                            performContext.SetJobParameter("Exception Message", exception.Message);
                            performContext.SetJobParameter("exception", exception);
                            performContext.SetJobParameter("Next Retry in", TimeSpan.FromSeconds(15));
                        }

                        PrintDebugLine(exception, timeSpan, retryCount);
                        await onRetry(exception, timeSpan, retryCount); // Call the retry notification logic

                        if (retryCount == RetryCountLimit && !string.IsNullOrEmpty(jobId))
                        {
                            var client = new BackgroundJobClient();
                            client.Delete(jobId);
                        }
                    });


            AsyncFallbackPolicy fallbackPolicy = Policy
            .Handle<Exception>() // Catch exceptions that are in the list
            .FallbackAsync(
                fallbackAction: async (cancellationToken) =>
                {
                    Debug.WriteLine("Fallback executed for non-retryable exception.");
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        var client = new BackgroundJobClient();
                        client.Delete(jobId);
                    }
                    await Task.CompletedTask;
                },
                onFallbackAsync: async (exception) =>
                {
                    await onFallback(exception); // Log or handle the exception
                    Debug.WriteLine($"Fallback due to exception: {exception.GetType()} - {exception.Message}");
                });

            // Combine the retry and fallback policies
            var policyWrap = fallbackPolicy.WrapAsync(retryPolicy);
            await policyWrap.ExecuteAsync(action);
            OnSuccess(performContext!);
        }

        private static void OnSuccess(PerformContext performContext)
        {
            if (performContext != null)
            {
                // Clear or reset job parameters upon success
                performContext.SetJobParameter("Status", "Success");
                performContext.SetJobParameter("Retry Attempt", CurrentRetryAttemp);
                performContext.SetJobParameter("Exception Message", null);
                performContext.SetJobParameter("exception", null);
                performContext.SetJobParameter("Next Retry in", null);
            }

            Debug.WriteLine("Action executed successfully.");
        }

        private static void PrintDebugLine(Exception exception, TimeSpan timeSpan, int retryCount)
        {
            Debug.WriteLine(new string('=', 100));
            Debug.WriteLine($"Retry {retryCount}.\nTime: {timeSpan}\nError: {exception.GetType()} -- {exception.Message}");
        }
    }

    
}
