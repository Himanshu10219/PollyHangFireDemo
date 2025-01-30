using Hangfire.Server;
using System.Diagnostics;

namespace PollyHangFireDemo
{
    public static class Helper
    {
        public static async Task MoveFileToDestinationAsync(PerformContext performContext)
        {
            await RetryPolicies.ExecuteWithRetryAsync(
            performContext: performContext,
            action: async () =>
            {
                string rootFolderPath = @"H:\\Testing\\Source\\File.txt";
                string destinationPath = @"H:\\Testing\\Destination\\File.txt";

                // Move the file
                System.IO.File.Move(rootFolderPath, destinationPath);
                Debug.WriteLine("File successfully moved.");
            },
            onRetry: async (exception, timeSpan, retryCount) =>
            {
                Debug.WriteLine(new string('=', 100));
                Debug.WriteLine($"Retry {retryCount}.\nTime: {timeSpan}\nError: {exception.GetType()} -- {exception.Message}");
            },
            onFallback: async (exception) =>
            {
                Debug.WriteLine(new string('=', 100));
                Debug.WriteLine($"Error: {exception.GetType()} -- {exception.Message}");
                await Task.CompletedTask;
            }
            );
        }
    }
}
