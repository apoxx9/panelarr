using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.ComicVine
{
    [TestFixture]
    public class ComicVineProviderFixture : CoreTest<ComicVineProvider>
    {
        private static List<ComicVineIssueSummary> Issues(params string[] coverDates)
        {
            var list = new List<ComicVineIssueSummary>();
            foreach (var d in coverDates)
            {
                list.Add(new ComicVineIssueSummary { CoverDate = d });
            }

            return list;
        }

        // ComicVine has no ongoing/ended flag - status is derived from the
        // most recent cover date so the refresh cadence can tier on it
        [Test]
        public void status_is_continuing_when_latest_issue_is_recent()
        {
            var recent = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd");

            ComicVineProvider.DeriveStatus(Issues("2015-01-01", recent)).Should().Be("continuing");
        }

        [Test]
        public void status_is_ended_when_latest_issue_is_old()
        {
            ComicVineProvider.DeriveStatus(Issues("2015-01-01", "2016-06-01")).Should().Be("ended");
        }

        [Test]
        public void status_is_continuing_when_no_issue_has_a_date()
        {
            // no evidence either way - stay on the safe (daily) side
            ComicVineProvider.DeriveStatus(Issues(null, "")).Should().Be("continuing");
            ComicVineProvider.DeriveStatus(Issues()).Should().Be("continuing");
        }

        // Null means "no delta support — use staleness checks". An empty list
        // would mean "nothing changed" and make a delta-based refresh skip
        // every series.
        [Test]
        public void changed_series_should_be_null_not_empty()
        {
            Assert.That(Subject.GetChangedSeries(0), Is.Null);
        }

        [Test]
        public void new_releases_should_be_null_not_empty()
        {
            Assert.That(Subject.GetNewReleases(0), Is.Null);
        }
    }
}
