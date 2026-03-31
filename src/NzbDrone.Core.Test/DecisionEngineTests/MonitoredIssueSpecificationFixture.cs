using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class MonitoredIssueSpecificationFixture : CoreTest<MonitoredIssueSpecification>
    {
        private MonitoredIssueSpecification _monitoredIssueSpecification;

        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private Series _fakeSeries;
        private Issue _firstIssue;
        private Issue _secondIssue;

        [SetUp]
        public void Setup()
        {
            _monitoredIssueSpecification = Mocker.Resolve<MonitoredIssueSpecification>();

            _fakeSeries = Builder<Series>.CreateNew()
                .With(c => c.Monitored = true)
                .Build();

            _firstIssue = new Issue { Monitored = true };
            _secondIssue = new Issue { Monitored = true };

            var singleIssueList = new List<Issue> { _firstIssue };
            var doubleIssueList = new List<Issue> { _firstIssue, _secondIssue };

            _parseResultMulti = new RemoteIssue
            {
                Series = _fakeSeries,
                Issues = doubleIssueList
            };

            _parseResultSingle = new RemoteIssue
            {
                Series = _fakeSeries,
                Issues = singleIssueList
            };
        }

        private void WithFirstIssueUnmonitored()
        {
            _firstIssue.Monitored = false;
        }

        private void WithSecondIssueUnmonitored()
        {
            _secondIssue.Monitored = false;
        }

        [Test]
        public void setup_should_return_monitored_book_should_return_true()
        {
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void not_monitored_author_should_be_skipped()
        {
            _fakeSeries.Monitored = false;
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_book_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void both_books_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            WithSecondIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_first_book_not_monitored_should_return_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void only_second_book_not_monitored_should_return_false()
        {
            WithSecondIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_for_single_book_search()
        {
            _fakeSeries.Monitored = false;
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_book_is_not_monitored_and_monitoredEpisodesOnly_flag_is_false()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria { MonitoredIssuesOnly = false }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_book_is_not_monitored_and_monitoredEpisodesOnly_flag_is_true()
        {
            WithFirstIssueUnmonitored();
            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria { MonitoredIssuesOnly = true }).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_all_books_are_not_monitored_for_discography_pack_release()
        {
            WithSecondIssueUnmonitored();
            _parseResultMulti.ParsedIssueInfo = new ParsedIssueInfo()
            {
                Discography = true
            };

            _monitoredIssueSpecification.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }
    }
}
