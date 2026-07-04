using System;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource.ComicVine
{
    [TestFixture]
    public class ComicVineRateLimiterFixture : TestBase
    {
        [Test]
        public void should_not_block_while_bucket_has_tokens()
        {
            var limiter = new ComicVineRateLimiter(TestLogger, 5, TimeSpan.FromHours(1));

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 5; i++)
            {
                limiter.WaitForToken();
            }

            sw.Stop();
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void should_drip_a_token_after_one_interval_instead_of_waiting_full_period()
        {
            // 4 tokens per 2s => one token drips every 500ms.
            var limiter = new ComicVineRateLimiter(TestLogger, 4, TimeSpan.FromSeconds(2));

            for (var i = 0; i < 4; i++)
            {
                limiter.WaitForToken();
            }

            var sw = Stopwatch.StartNew();
            limiter.WaitForToken();
            sw.Stop();

            // Should wait ~500ms for the next dripped token, not the full 2s refill period.
            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(300));
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1500));
        }

        [Test]
        public void should_accrue_tokens_while_idle()
        {
            // 4 tokens per 1s => one token drips every 250ms.
            var limiter = new ComicVineRateLimiter(TestLogger, 4, TimeSpan.FromSeconds(1));

            for (var i = 0; i < 4; i++)
            {
                limiter.WaitForToken();
            }

            // Idle long enough for at least 2 tokens to accrue.
            System.Threading.Thread.Sleep(600);

            var sw = Stopwatch.StartNew();
            limiter.WaitForToken();
            limiter.WaitForToken();
            sw.Stop();

            sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        }

        [Test]
        public void should_cap_accrued_tokens_at_bucket_size()
        {
            // 2 tokens per 400ms => one token drips every 200ms.
            var limiter = new ComicVineRateLimiter(TestLogger, 2, TimeSpan.FromMilliseconds(400));

            // Idle well beyond a full refill period; bucket must not exceed 2.
            System.Threading.Thread.Sleep(1200);

            limiter.WaitForToken();
            limiter.WaitForToken();

            var sw = Stopwatch.StartNew();
            limiter.WaitForToken();
            sw.Stop();

            // Third call must block for a drip; if the bucket over-filled it would return instantly.
            sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
        }

        [Test]
        public void should_default_to_200_tokens_per_hour()
        {
            var limiter = new ComicVineRateLimiter(TestLogger);

            limiter.BucketSize.Should().Be(200);
        }
    }
}
