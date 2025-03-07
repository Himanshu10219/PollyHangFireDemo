using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire;

namespace PollyHangFireDemo.HangfireUtils.Filters
{
    public class CustomRetryFilter : JobFilterAttribute, IElectStateFilter
    {
        private readonly int _maxAttempts;
        private readonly int[] _retryDelays;
        private readonly Action<Exception, TimeSpan, int> _onRetry;

        public CustomRetryFilter(int maxAttempts, Action<Exception, TimeSpan, int> onRetry, int[] retryDelays = null)
        {
            _maxAttempts = maxAttempts;
            _onRetry = onRetry;
            _retryDelays = retryDelays?.Length > 0 ? retryDelays : new[] { 5, 10, 60 };
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }

        public void OnStateElection(ElectStateContext context)
        {
            var connection = JobStorage.Current.GetConnection();
            if (context.CandidateState is not FailedState failedState)
                return;

            int retryCount = GetRetryCount(connection, context.JobId) ;

            if (retryCount >= _maxAttempts)
                return;

            int currentAttempt = retryCount + 1;
            TimeSpan retryDelay = GetRetryDelay(currentAttempt);

            // Update retry 
            connection.SetJobParameter(context.JobId, "RetryCount", currentAttempt.ToString());
            connection.SetJobParameter(context.JobId, "Error", failedState.Exception.Message ?? string.Empty);
            connection.SetJobParameter(context.JobId, "NextRetryIn", retryDelay.ToString());
            connection.SetJobParameter(context.JobId, "Attempts", _maxAttempts.ToString());

            // Schedule job retry
            context.CandidateState = new ScheduledState(retryDelay);

            // Invoke retry callback
            _onRetry?.Invoke(failedState.Exception, retryDelay, currentAttempt);
        }

        private static int GetRetryCount(IStorageConnection connection, string jobId)
        {
            return int.TryParse(connection.GetJobParameter(jobId, "RetryCount"), out int count) ? count : 0;
        }

        private TimeSpan GetRetryDelay(int attempt)
        {
            return TimeSpan.FromSeconds(attempt <= _retryDelays.Length ? _retryDelays[attempt - 1] : _retryDelays.Last());
        }
    }
}
