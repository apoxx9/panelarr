using System;
using System.Threading;

namespace NzbDrone.Core.MetadataSource.Metron
{
    /// <summary>
    /// Token bucket rate limiter for Metron API.
    /// Limits: 20 requests/minute and 5000 requests/day.
    /// </summary>
    public class MetronRateLimiter
    {
        // Per-minute bucket: 20 requests/60 seconds
        private const int PerMinuteLimit = 20;
        private const int PerMinuteWindowSeconds = 60;

        // Per-day bucket: 5000 requests/86400 seconds
        private const int PerDayLimit = 5000;
        private const int PerDayWindowSeconds = 86400;

        private readonly object _lock = new object();

        private int _minuteTokens = PerMinuteLimit;
        private int _dayTokens = PerDayLimit;
        private DateTime _minuteWindowStart = DateTime.UtcNow;
        private DateTime _dayWindowStart = DateTime.UtcNow;

        public void WaitForToken()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Refill minute bucket
                if ((now - _minuteWindowStart).TotalSeconds >= PerMinuteWindowSeconds)
                {
                    _minuteTokens = PerMinuteLimit;
                    _minuteWindowStart = now;
                }

                // Refill day bucket
                if ((now - _dayWindowStart).TotalSeconds >= PerDayWindowSeconds)
                {
                    _dayTokens = PerDayLimit;
                    _dayWindowStart = now;
                }

                // Wait if minute bucket empty
                if (_minuteTokens <= 0)
                {
                    var waitMs = (int)((PerMinuteWindowSeconds - (now - _minuteWindowStart).TotalSeconds) * 1000) + 100;
                    if (waitMs > 0)
                    {
                        Monitor.Exit(_lock);
                        Thread.Sleep(waitMs);
                        Monitor.Enter(_lock);
                    }

                    _minuteTokens = PerMinuteLimit;
                    _minuteWindowStart = DateTime.UtcNow;
                }

                _minuteTokens--;
                _dayTokens = Math.Max(0, _dayTokens - 1);
            }
        }
    }
}
