using System;
using System.Threading;
using NLog;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public enum ComicVineRequestPriority
    {
        // Scheduled / bulk work (library refresh). Yields to interactive.
        Bulk = 0,

        // A person is waiting on the result (add-series lookup, manual
        // search, resolve). Takes the next token ahead of any bulk caller.
        Interactive = 1
    }

    /// <summary>
    /// Token-bucket rate limiter for ComicVine API: 200 requests/hour.
    /// Tokens drip continuously (one per ~18s) rather than refilling in an
    /// hourly burst, so bulk operations degrade to a steady crawl instead of
    /// stalling for up to an hour. Two lanes: interactive callers are served
    /// before bulk callers whenever they are waiting, so a scheduled refresh
    /// that has drained the bucket cannot hold an add-series lookup hostage
    /// for minutes. Logs when requests start being throttled.
    /// </summary>
    public class ComicVineRateLimiter
    {
        private const int MaxTokens = 200;
        private static readonly TimeSpan WarnInterval = TimeSpan.FromMinutes(5);

        // Ambient priority for the current async/thread flow, set by the
        // caller that knows whether a person is waiting (see
        // ComicVineRequestScope). Defaults to Bulk.
        private static readonly AsyncLocal<ComicVineRequestPriority> AmbientPriority = new AsyncLocal<ComicVineRequestPriority>();

        private readonly TimeSpan _tokenInterval;
        private readonly Logger _logger;
        private readonly object _lock = new object();
        private int _tokens;
        private int _interactiveWaiting;
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

        public static ComicVineRequestPriority CurrentPriority
        {
            get => AmbientPriority.Value;
            internal set => AmbientPriority.Value = value;
        }

        public void WaitForToken()
        {
            WaitForToken(CurrentPriority);
        }

        public void WaitForToken(ComicVineRequestPriority priority)
        {
            var interactive = priority == ComicVineRequestPriority.Interactive;

            lock (_lock)
            {
                if (interactive)
                {
                    _interactiveWaiting++;
                }

                try
                {
                    Drip();

                    // A bulk caller must also stand aside while any interactive
                    // caller is waiting, even if a token is available right now.
                    while (_tokens <= 0 || (!interactive && _interactiveWaiting > 0))
                    {
                        var nextToken = _lastDrip.Add(_tokenInterval);
                        var waitMs = (int)(nextToken - DateTime.UtcNow).TotalMilliseconds + 50;

                        if (_tokens <= 0 && waitMs > 0)
                        {
                            LogThrottled(waitMs);
                            Monitor.Wait(_lock, waitMs);
                        }
                        else
                        {
                            // Tokens exist but an interactive caller has dibs -
                            // wait to be pulsed when it takes its token.
                            Monitor.Wait(_lock, 250);
                        }

                        Drip();
                    }

                    _tokens--;
                }
                finally
                {
                    if (interactive)
                    {
                        _interactiveWaiting--;
                    }

                    // Wake the others so a bulk caller re-checks the gate and
                    // the next interactive caller (if any) can take its turn.
                    Monitor.PulseAll(_lock);
                }
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

    /// <summary>
    /// Marks the current flow as interactive for the duration of the scope:
    /// every ComicVine call made inside it takes the fast lane.
    /// </summary>
    public sealed class ComicVineRequestScope : IDisposable
    {
        private readonly ComicVineRequestPriority _previous;

        private ComicVineRequestScope(ComicVineRequestPriority priority)
        {
            _previous = ComicVineRateLimiter.CurrentPriority;
            ComicVineRateLimiter.CurrentPriority = priority;
        }

        public static ComicVineRequestScope Interactive()
        {
            return new ComicVineRequestScope(ComicVineRequestPriority.Interactive);
        }

        public void Dispose()
        {
            ComicVineRateLimiter.CurrentPriority = _previous;
        }
    }
}
