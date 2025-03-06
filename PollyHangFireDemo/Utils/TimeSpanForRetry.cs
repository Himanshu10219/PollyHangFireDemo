using System;

namespace PollyHangFireDemo.Utils
{
    public class TimeSpanForRetry : ITimeSpanForRetry
    {
        public TimeSpan GetSleepDuration(int retryAttempt)
          => retryAttempt switch
          {
              0 => TimeSpan.Zero,
              1 => TimeSpan.FromMinutes(5),
              2 => TimeSpan.FromMinutes(30),
              3 => TimeSpan.FromHours(1),
              4 => TimeSpan.FromHours(12),
              5 => TimeSpan.FromDays(1),
              6 => TimeSpan.FromDays(2),
              _ => throw new ArgumentOutOfRangeException(nameof(retryAttempt), retryAttempt, "Must be in the range of [0, 6]")
          };
      }
    
}
