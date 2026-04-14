using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class SeriesSpecificationFixture : TestBase<SeriesSpecification>
    {
        private Series _author1;
        private Series _author2;
        private RemoteIssue _remoteIssue = new RemoteIssue();
        private SearchCriteriaBase _searchCriteria = new IssueSearchCriteria();

        [SetUp]
        public void Setup()
        {
            _author1 = Builder<Series>.CreateNew().With(s => s.Id = 1).Build();
            _author2 = Builder<Series>.CreateNew().With(s => s.Id = 2).Build();

            _remoteIssue.Series = _author1;
        }

        [Test]
        public void should_return_false_if_author_doesnt_match()
        {
            _searchCriteria.Series = _author2;

            Subject.IsSatisfiedBy(_remoteIssue, _searchCriteria).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_series_ids_match()
        {
            _searchCriteria.Series = _author1;

            Subject.IsSatisfiedBy(_remoteIssue, _searchCriteria).Accepted.Should().BeTrue();
        }
    }
}
