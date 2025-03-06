using System;

namespace PollyHangFireDemo.Utils
{
    public interface ITimeSpanForRetry
    {
        public TimeSpan GetSleepDuration(int retryAttempt);
    }
}
