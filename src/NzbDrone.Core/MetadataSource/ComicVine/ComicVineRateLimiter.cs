using System;
using System.Threading;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    /// <summary>
    /// Token-bucket rate limiter for ComicVine API: 200 requests/hour.
    /// </summary>
    public class ComicVineRateLimiter
    {
        private const int MaxRequestsPerHour = 200;
        private readonly TimeSpan _refillInterval = TimeSpan.FromHours(1);
        private readonly object _lock = new object();
        private int _tokens = MaxRequestsPerHour;
        private DateTime _lastRefill = DateTime.UtcNow;

        public void WaitForToken()
        {
            lock (_lock)
            {
                Refill();

                while (_tokens <= 0)
                {
                    // Calculate how long until next token is available
                    var waitMs = (int)(_lastRefill.Add(_refillInterval) - DateTime.UtcNow).TotalMilliseconds + 100;
                    if (waitMs > 0)
                    {
                        Monitor.Wait(_lock, waitMs);
                    }

                    Refill();
                }

                _tokens--;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            if (now >= _lastRefill.Add(_refillInterval))
            {
                _tokens = MaxRequestsPerHour;
                _lastRefill = now;
            }
        }
    }
}
