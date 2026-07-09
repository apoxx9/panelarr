using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class MultiIssuePackSpecificationFixture : CoreTest<MultiIssuePackSpecification>
    {
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            _remoteIssue = new RemoteIssue
            {
                Release = new ReleaseInfo { Title = "0-Day Week of 2025.10.22 [ENG / CBR CBZ] [VIP]" },
                ParsedIssueInfo = new ParsedIssueInfo()
            };
        }

        private SearchCriteriaBase GivenSearchCriteria(bool interactive)
        {
            return new IssueSearchCriteria { InteractiveSearch = interactive };
        }

        [Test]
        public void should_accept_single_issue_release_for_rss()
        {
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_pack_for_rss()
        {
            _remoteIssue.ParsedIssueInfo.IsMultiIssue = true;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_pack_for_automatic_search()
        {
            _remoteIssue.ParsedIssueInfo.IsMultiIssue = true;

            Subject.IsSatisfiedBy(_remoteIssue, GivenSearchCriteria(false)).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_pack_for_interactive_search()
        {
            _remoteIssue.ParsedIssueInfo.IsMultiIssue = true;

            Subject.IsSatisfiedBy(_remoteIssue, GivenSearchCriteria(true)).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_parsed_info_is_missing()
        {
            _remoteIssue.ParsedIssueInfo = null;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }
    }
}
