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
                // Loop instead of one-shot sleeps: windows are only refilled when
                // they have actually elapsed, and waking threads re-check state so
                // concurrent sleepers can't grant themselves fresh windows.
                while (true)
                {
                    var now = DateTime.UtcNow;

                    if ((now - _minuteWindowStart).TotalMilliseconds >= PerMinuteWindowMs)
                    {
                        _minuteTokens = PerMinuteLimit;
                        _minuteWindowStart = now;
                    }

                    if ((now - _dayWindowStart).TotalHours >= 24)
                    {
                        _dayTokens = PerDayLimit;
                        _dayWindowStart = now;
                    }

                    if (_minuteTokens > 0 && _dayTokens > 0)
                    {
                        _minuteTokens--;
                        _dayTokens--;
                        return;
                    }

                    // Sleep toward the earliest window that frees a token, in
                    // capped slices so day-limit waits stay responsive.
                    int waitMs;
                    if (_minuteTokens <= 0 && _dayTokens > 0)
                    {
                        waitMs = (int)(PerMinuteWindowMs - (now - _minuteWindowStart).TotalMilliseconds) + 100;
                    }
                    else
                    {
                        waitMs = (int)((24d * 3600 * 1000) - (now - _dayWindowStart).TotalMilliseconds) + 100;
                    }

                    waitMs = Math.Clamp(waitMs, 100, 60_000);

                    _semaphore.Release();
                    Thread.Sleep(waitMs);
                    _semaphore.Wait();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
