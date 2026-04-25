using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class GetIssuesFixture : CoreTest<ParsingService>
    {
        [Test]
        public void should_not_fail_if_search_criteria_contains_multiple_books_with_the_same_name()
        {
            var series = Builder<Series>.CreateNew().Build();
            var issues = Builder<Issue>.CreateListOfSize(2).All().With(x => x.Title = "IdenticalTitle").Build().ToList();
            var criteria = new IssueSearchCriteria
            {
                Series = series,
                Issues = issues
            };

            var parsed = new ParsedIssueInfo
            {
                IssueTitle = "IdenticalTitle"
            };

            Subject.GetIssues(parsed, series, criteria).Should().BeEquivalentTo(new List<Issue>());

            Mocker.GetMock<IIssueService>()
                .Verify(s => s.FindByTitle(series.SeriesMetadataId, "IdenticalTitle"), Times.Once());
        }
    }
}
