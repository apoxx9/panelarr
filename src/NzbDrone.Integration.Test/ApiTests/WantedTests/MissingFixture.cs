using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using Panelarr.Api.V1.RootFolders;

namespace NzbDrone.Integration.Test.ApiTests.WantedTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class MissingFixture : IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            // Add a root folder
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = SeriesRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });
        }

        [Test]
        [Order(0)]
        public void missing_should_be_empty()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var result = WantedMissing.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(1)]
        public void missing_should_have_monitored_items()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", true);

            var result = WantedMissing.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().NotBeEmpty();
        }

        [Test]
        [Order(1)]
        public void missing_should_have_author()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", true);

            var result = WantedMissing.GetPagedIncludeSeries(0, 15, "releaseDate", "desc", includeSeries: true);

            result.Records.First().Series.Should().NotBeNull();
            result.Records.First().Series.SeriesName.Should().Be("Andrew Hunter Murray");
        }

        [Test]
        [Order(1)]
        public void missing_should_not_have_author()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", true);

            var result = WantedMissing.GetPagedIncludeSeries(0, 15, "releaseDate", "desc", includeSeries: false);

            result.Records.First().Series.Should().BeNull();
        }

        [Test]
        [Order(1)]
        public void missing_should_not_have_unmonitored_items()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", false);

            var result = WantedMissing.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(2)]
        public void missing_should_have_unmonitored_items()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", false);

            var result = WantedMissing.GetPaged(0, 15, "releaseDate", "desc", "monitored", false);

            result.Records.Should().NotBeEmpty();
        }
    }
}
