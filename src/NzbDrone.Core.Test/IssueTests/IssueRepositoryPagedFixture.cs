using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class IssueRepositoryPagedFixture : DbTest<IssueRepository, Issue>
    {
        private void GivenIssues(int count)
        {
            var issues = Builder<Issue>.CreateListOfSize(count)
                .All()
                .With(i => i.SeriesMetadataId = 1)
                .BuildListOfNew();

            Db.InsertMany(issues);
        }

        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 1)]
        public void get_paged_should_return_page_ordered_by_id(int page, int expectedCount)
        {
            GivenIssues(5);

            var result = Subject.GetPaged(new PagingSpec<Issue> { Page = page, PageSize = 2 });

            result.TotalRecords.Should().Be(5);
            result.Records.Should().HaveCount(expectedCount);
            result.Records.Select(i => i.Id).Should().BeEquivalentTo(
                AllStoredModels.OrderBy(i => i.Id).Skip((page - 1) * 2).Take(2).Select(i => i.Id));
        }

        [Test]
        public void get_paged_should_return_empty_page_beyond_last()
        {
            GivenIssues(3);

            var result = Subject.GetPaged(new PagingSpec<Issue> { Page = 5, PageSize = 2 });

            result.TotalRecords.Should().Be(3);
            result.Records.Should().BeEmpty();
        }
    }
}
