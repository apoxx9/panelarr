using System;
using System.Threading;

namespace NzbDrone.Core.MetadataSource.Metron
{
    /// <summary>
    /// Token bucket rate limiter for Metron API.
    /// Limits: 20 requests/minute and 5000 requests/day.
    /// Thread-safe: uses SemaphoreSlim to avoid lock release/reacquire race conditions.
    /// </summary>
    public class MetronRateLimiter
    {
        private const int PerMinuteLimit = 20;
        private const int PerMinuteWindowMs = 60_000;
        private const int PerDayLimit = 5000;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private int _minuteTokens = PerMinuteLimit;
        private int _dayTokens = PerDayLimit;
        private DateTime _minuteWindowStart = DateTime.UtcNow;
        private DateTime _dayWindowStart = DateTime.UtcNow;

        public void WaitForToken()
        {
            _semaphore.Wait();
            try
            {
                var now = DateTime.UtcNow;

                // Refill minute bucket if window elapsed
                if ((now - _minuteWindowStart).TotalMilliseconds >= PerMinuteWindowMs)
                {
                    _minuteTokens = PerMinuteLimit;
                    _minuteWindowStart = now;
                }

                // Refill day bucket if 24h elapsed
                if ((now - _dayWindowStart).TotalHours >= 24)
                {
                    _dayTokens = PerDayLimit;
                    _dayWindowStart = now;
                }

                // Wait if minute bucket empty
                if (_minuteTokens <= 0)
                {
                    var waitMs = (int)(PerMinuteWindowMs - (now - _minuteWindowStart).TotalMilliseconds) + 100;
                    if (waitMs > 0)
                    {
                        _semaphore.Release();
                        Thread.Sleep(waitMs);
                        _semaphore.Wait();
                    }

                    // After sleeping, reset the window
                    _minuteTokens = PerMinuteLimit;
                    _minuteWindowStart = DateTime.UtcNow;
                }

                // Wait if daily bucket empty
                if (_dayTokens <= 0)
                {
                    var waitMs = (int)((24 * 3600 * 1000) - (DateTime.UtcNow - _dayWindowStart).TotalMilliseconds) + 100;
                    if (waitMs > 0 && waitMs < 3600_000)
                    {
                        _semaphore.Release();
                        Thread.Sleep(Math.Min(waitMs, 60_000));
                        _semaphore.Wait();
                    }

                    // Check if window rolled over
                    if ((DateTime.UtcNow - _dayWindowStart).TotalHours >= 24)
                    {
                        _dayTokens = PerDayLimit;
                        _dayWindowStart = DateTime.UtcNow;
                    }
                }

                _minuteTokens--;
                _dayTokens--;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
