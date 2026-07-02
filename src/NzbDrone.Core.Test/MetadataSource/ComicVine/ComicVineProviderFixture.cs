using NUnit.Framework;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.ComicVine
{
    [TestFixture]
    public class ComicVineProviderFixture : CoreTest<ComicVineProvider>
    {
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
