using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedIssuesFixture : DbTest<CleanupOrphanedIssues, Issue>
    {
        [Test]
        public void should_delete_orphaned_books()
        {
            var issue = Builder<Issue>.CreateNew()
                .BuildNew();

            Db.Insert(issue);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_books()
        {
            var series = Builder<Series>.CreateNew()
                .With(e => e.Metadata = new SeriesMetadata { Id = 1 })
                .BuildNew();

            Db.Insert(series);

            var issues = Builder<Issue>.CreateListOfSize(2)
                .TheFirst(1)
                .With(e => e.SeriesMetadataId = series.Metadata.Value.Id)
                .BuildListOfNew();

            Db.InsertMany(issues);
            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(e => e.SeriesMetadataId == series.Metadata.Value.Id);
        }
    }
}
