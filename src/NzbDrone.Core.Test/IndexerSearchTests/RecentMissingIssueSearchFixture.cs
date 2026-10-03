using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class RecentMissingIssueSearchFixture : CoreTest<IssueSearchService>
    {
        private List<Issue> _missing;
        private List<int> _searched;

        [SetUp]
        public void Setup()
        {
            _missing = new List<Issue>();
            _searched = new List<int>();

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.IssuesWithoutFiles(It.IsAny<PagingSpec<Issue>>()))
                  .Returns((PagingSpec<Issue> spec) =>
                  {
                      spec.Records = _missing;
                      return spec;
                  });

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(new List<Queue.Queue>());

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.IssueSearch(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                  .Callback((int id, bool a, bool b, bool c) => _searched.Add(id))
                  .ReturnsAsync(new List<DownloadDecision>());

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                  .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>()));
        }

        private static Issue MissingIssue(int id, int daysAgoReleased)
        {
            return new Issue { Id = id, ReleaseDate = DateTime.UtcNow.AddDays(-daysAgoReleased), Monitored = true };
        }

        [Test]
        public void should_search_only_recently_released_issues()
        {
            _missing.Add(MissingIssue(1, 2));
            _missing.Add(MissingIssue(2, 13));
            _missing.Add(MissingIssue(3, 40));
            _missing.Add(new Issue { Id = 4, ReleaseDate = null, Monitored = true });

            Subject.Execute(new RecentMissingIssueSearchCommand());

            _searched.Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Test]
        public void should_cap_the_number_of_searches()
        {
            for (var i = 1; i <= RecentMissingIssueSearchCommand.MaxIssues + 10; i++)
            {
                _missing.Add(MissingIssue(i, 1));
            }

            Subject.Execute(new RecentMissingIssueSearchCommand());

            _searched.Should().HaveCount(RecentMissingIssueSearchCommand.MaxIssues);
        }

        [Test]
        public void should_skip_issues_already_in_the_queue()
        {
            _missing.Add(MissingIssue(1, 2));
            _missing.Add(MissingIssue(2, 2));

            Mocker.GetMock<IQueueService>()
                  .Setup(s => s.GetQueue())
                  .Returns(new List<Queue.Queue> { new Queue.Queue { Issue = new Issue { Id = 2 } } });

            Subject.Execute(new RecentMissingIssueSearchCommand());

            _searched.Should().BeEquivalentTo(new[] { 1 });
        }

        [Test]
        public void should_do_nothing_when_nothing_recent_is_missing()
        {
            _missing.Add(MissingIssue(1, 100));

            Subject.Execute(new RecentMissingIssueSearchCommand());

            _searched.Should().BeEmpty();
        }
    }
}
