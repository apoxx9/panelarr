using System;
using System.Threading;
using NLog;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    /// <summary>
    /// Token-bucket rate limiter for ComicVine API: 200 requests/hour.
    /// Tokens drip continuously (one per ~18s) rather than refilling in an
    /// hourly burst, so bulk operations degrade to a steady crawl instead of
    /// stalling for up to an hour. Logs when requests start being throttled.
    /// </summary>
    public class ComicVineRateLimiter
    {
        private const int MaxTokens = 200;
        private static readonly TimeSpan WarnInterval = TimeSpan.FromMinutes(5);

        private readonly TimeSpan _tokenInterval;
        private readonly Logger _logger;
        private readonly object _lock = new object();
        private int _tokens;
        private DateTime _lastDrip = DateTime.UtcNow;
        private DateTime _lastWarn = DateTime.MinValue;

        public ComicVineRateLimiter(Logger logger)
            : this(logger, MaxTokens, TimeSpan.FromHours(1))
        {
        }

        // Test constructor: smaller bucket / faster drip.
        public ComicVineRateLimiter(Logger logger, int maxTokens, TimeSpan refillPeriod)
        {
            _logger = logger;
            BucketSize = maxTokens;
            _tokens = maxTokens;
            _tokenInterval = TimeSpan.FromTicks(refillPeriod.Ticks / maxTokens);
        }

        public int BucketSize { get; }

        public void WaitForToken()
        {
            lock (_lock)
            {
                Drip();

                while (_tokens <= 0)
                {
                    var nextToken = _lastDrip.Add(_tokenInterval);
                    var waitMs = (int)(nextToken - DateTime.UtcNow).TotalMilliseconds + 50;

                    if (waitMs > 0)
                    {
                        LogThrottled(waitMs);
                        Monitor.Wait(_lock, waitMs);
                    }

                    Drip();
                }

                _tokens--;
            }
        }

        private void Drip()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastDrip;

            if (elapsed < _tokenInterval)
            {
                return;
            }

            var newTokens = (int)(elapsed.Ticks / _tokenInterval.Ticks);
            _tokens += newTokens;

            if (_tokens >= BucketSize)
            {
                _tokens = BucketSize;
                _lastDrip = now;
            }
            else
            {
                // Advance by whole tokens only, preserving fractional accrual.
                _lastDrip = _lastDrip.AddTicks(newTokens * _tokenInterval.Ticks);
            }
        }

        private void LogThrottled(int waitMs)
        {
            var now = DateTime.UtcNow;

            if (now - _lastWarn >= WarnInterval)
            {
                _lastWarn = now;
                _logger?.Warn("ComicVine API rate limit reached ({0} requests per {1:0.#}h); throttling to one request per {2:0.#}s",
                    BucketSize,
                    (BucketSize * _tokenInterval).TotalHours,
                    _tokenInterval.TotalSeconds);
            }
            else
            {
                _logger?.Debug("ComicVine rate limit: waiting {0:0.#}s for next request slot", waitMs / 1000.0);
            }
        }
    }
}
