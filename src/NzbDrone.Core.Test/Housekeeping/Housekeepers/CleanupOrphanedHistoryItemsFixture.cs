using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedHistoryItemsFixture : DbTest<CleanupOrphanedHistoryItems, EntityHistory>
    {
        private Series _series;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .BuildNew();

            _issue = Builder<Issue>.CreateNew()
                .BuildNew();
        }

        private void GivenSeries()
        {
            Db.Insert(_series);
        }

        private void GivenIssue()
        {
            Db.Insert(_issue);
        }

        [Test]
        public void should_delete_orphaned_items_by_author()
        {
            GivenIssue();

            var history = Builder<EntityHistory>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.IssueId = _issue.Id)
                                                  .BuildNew();
            Db.Insert(history);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_delete_orphaned_items_by_book()
        {
            GivenSeries();

            var history = Builder<EntityHistory>.CreateNew()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.SeriesId = _series.Id)
                                                  .BuildNew();
            Db.Insert(history);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_data_by_author()
        {
            GivenSeries();
            GivenIssue();

            var history = Builder<EntityHistory>.CreateListOfSize(2)
                                                  .All()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.IssueId = _issue.Id)
                                                  .TheFirst(1)
                                                  .With(h => h.SeriesId = _series.Id)
                                                  .BuildListOfNew();

            Db.InsertMany(history);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(h => h.SeriesId == _series.Id);
        }

        [Test]
        public void should_not_delete_unorphaned_data_by_book()
        {
            GivenSeries();
            GivenIssue();

            var history = Builder<EntityHistory>.CreateListOfSize(2)
                                                  .All()
                                                  .With(h => h.Quality = new QualityModel())
                                                  .With(h => h.SeriesId = _series.Id)
                                                  .TheFirst(1)
                                                  .With(h => h.IssueId = _issue.Id)
                                                  .BuildListOfNew();

            Db.InsertMany(history);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(h => h.IssueId == _issue.Id);
        }
    }
}
