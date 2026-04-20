using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Test.Common;
using Panelarr.Api.V1.Series;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class SeriesEditorFixture : IntegrationTest
    {
        private void GivenExistingSeries()
        {
            WaitForCompletion(() => Profiles.All().Count > 0);

            foreach (var name in new[] { "Alien Ant Farm", "Kiss" })
            {
                var newSeries = Series.Lookup(name).First();

                newSeries.QualityProfileId = 1;
                newSeries.Path = string.Format(@"C:\Test\{0}", name).AsOsAgnostic();

                Series.Post(newSeries);
            }
        }

        [Test]
        public void should_be_able_to_update_multiple_author()
        {
            GivenExistingSeries();

            var series = Series.All();

            var seriesEditor = new SeriesEditorResource
            {
                QualityProfileId = 2,
                SeriesIds = series.Select(o => o.Id).ToList()
            };

            var result = Series.Editor(seriesEditor);

            result.Should().HaveCount(2);
            result.TrueForAll(s => s.QualityProfileId == 2).Should().BeTrue();
        }
    }
}
