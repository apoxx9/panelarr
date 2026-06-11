using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedSeriesIssueLinksFixture : DbTest<CleanupOrphanedSeriesIssueLinks, SeriesGroupLink>
    {
        [Test]
        public void should_delete_links_with_orphaned_series_metadata()
        {
            var link = Builder<SeriesGroupLink>.CreateNew()
                .With(l => l.SeriesGroupId = 1)
                .With(l => l.SeriesMetadataId = 123)
                .BuildNew();

            Db.Insert(link);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_keep_links_with_existing_series_metadata()
        {
            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(m => m.Id = 0)
                .BuildNew();

            Db.Insert(metadata);

            var group = Builder<SeriesGroup>.CreateNew()
                .With(g => g.Id = 0)
                .BuildNew();

            Db.Insert(group);

            var link = Builder<SeriesGroupLink>.CreateNew()
                .With(l => l.SeriesGroupId = group.Id)
                .With(l => l.SeriesMetadataId = metadata.Id)
                .BuildNew();

            Db.Insert(link);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.First().SeriesMetadataId.Should().Be(metadata.Id);
        }

        [Test]
        public void should_delete_links_with_orphaned_series_group()
        {
            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(m => m.Id = 0)
                .BuildNew();

            Db.Insert(metadata);

            var link = Builder<SeriesGroupLink>.CreateNew()
                .With(l => l.SeriesGroupId = 456)
                .With(l => l.SeriesMetadataId = metadata.Id)
                .BuildNew();

            Db.Insert(link);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }
    }
}
