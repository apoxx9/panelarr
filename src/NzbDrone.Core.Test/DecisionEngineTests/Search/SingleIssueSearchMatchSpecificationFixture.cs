using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.Search;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.Search
{
    [TestFixture]
    public class SingleIssueSearchMatchSpecificationFixture : TestBase<SingleIssueSearchMatchSpecification>
    {
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            _remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = new ParsedIssueInfo()
            };
        }

        [Test]
        public void should_accept_when_no_search_criteria()
        {
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_not_a_single_issue_search()
        {
            var criteria = new SeriesSearchCriteria();

            Subject.IsSatisfiedBy(_remoteIssue, criteria).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_single_issue_release_with_null_issue_title()
        {
            // Comics releases are named by issue number, so IssueTitle is
            // routinely null. The old spec called IssueTitle.Any() here and
            // threw ArgumentNullException on every real single-issue search.
            _remoteIssue.ParsedIssueInfo.IssueTitle = null;
            _remoteIssue.ParsedIssueInfo.IsCollection = false;

            Subject.IsSatisfiedBy(_remoteIssue, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_single_issue_release_with_a_title()
        {
            _remoteIssue.ParsedIssueInfo.IssueTitle = "The Star";
            _remoteIssue.ParsedIssueInfo.IsCollection = false;

            Subject.IsSatisfiedBy(_remoteIssue, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_collection_pack_during_single_issue_search()
        {
            _remoteIssue.ParsedIssueInfo.IsCollection = true;

            Subject.IsSatisfiedBy(_remoteIssue, new IssueSearchCriteria()).Accepted.Should().BeFalse();
        }
    }
}
