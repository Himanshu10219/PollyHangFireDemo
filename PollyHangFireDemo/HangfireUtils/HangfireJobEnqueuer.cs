using Hangfire;
using Hangfire.States;
using PollyHangFireDemo.HangfireUtils.Filters;
using System.Linq.Expressions;

namespace PollyHangFireDemo.HangfireUtils
{
    public static class HangfireJobEnqueuer
    {
        public static void JobEnqueuer(
        this IBackgroundJobClient client,
        Expression<Func<Task>> action,
        string queue = "default",
        int maxRetries = 3,
        int[] retryDelays = null,
        Action<Exception, TimeSpan, int> onRetry = null)
        {
            retryDelays ??= new[] { 10, 20, 60 };
            var options = new EnqueuedState(queue);

            // Configure retry logic dynamically
            client.Create(action, new EnqueuedState(queue));

            // Wrap with a retry filter that calls the onRetry action
            GlobalJobFilters.Filters.Add(new CustomRetryFilter(maxRetries, onRetry, retryDelays));
        }
    }
}
