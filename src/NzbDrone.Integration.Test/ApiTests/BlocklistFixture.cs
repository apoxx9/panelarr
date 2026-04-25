using FluentAssertions;
using NUnit.Framework;
using Panelarr.Api.V1.Blocklist;
using Panelarr.Api.V1.Series;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class BlocklistFixture : IntegrationTest
    {
        private SeriesResource _series;

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_add_to_blocklist()
        {
            _series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            Blocklist.Post(new BlocklistResource
            {
                SeriesId = _series.Id,
                SourceTitle = "Blacklist - Issue 1 [2015 FLAC]"
            });
        }

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_get_all_blocklisted()
        {
            var result = Blocklist.GetPaged(0, 1000, "date", "desc");

            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(1);
            result.Records.Should().NotBeNullOrEmpty();
        }

        [Test]
        [Ignore("Adding to blocklist not supported")]
        public void should_be_able_to_remove_from_blocklist()
        {
            Blocklist.Delete(1);

            var result = Blocklist.GetPaged(0, 1000, "date", "desc");

            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(0);
        }
    }
}
