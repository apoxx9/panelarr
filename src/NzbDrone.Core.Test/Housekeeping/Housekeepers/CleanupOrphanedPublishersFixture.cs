using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedPublishersFixture : DbTest<CleanupOrphanedPublishers, Publisher>
    {
        [Test]
        public void should_delete_unreferenced_provider_publisher()
        {
            var publisher = Builder<Publisher>.CreateNew()
                .With(p => p.ForeignPublisherId = "cv:6")
                .BuildNew();

            Db.Insert(publisher);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_keep_user_created_publisher_without_references()
        {
            var publisher = Builder<Publisher>.CreateNew()
                .With(p => p.ForeignPublisherId = null)
                .BuildNew();

            Db.Insert(publisher);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }

        [Test]
        public void should_keep_referenced_provider_publisher()
        {
            var publisher = Builder<Publisher>.CreateNew()
                .With(p => p.Id = 0)
                .With(p => p.ForeignPublisherId = "cv:6")
                .BuildNew();

            Db.Insert(publisher);

            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(m => m.Id = 0)
                .With(m => m.PublisherId = publisher.Id)
                .BuildNew();

            Db.Insert(metadata);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
