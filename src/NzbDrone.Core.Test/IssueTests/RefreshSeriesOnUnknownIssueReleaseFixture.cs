using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class RefreshSeriesOnUnknownIssueReleaseFixture : CoreTest<RefreshSeriesOnUnknownIssueReleaseService>
    {
        private static DownloadDecision Rejected(Series series, List<Issue> issues)
        {
            var remote = new RemoteIssue
            {
                Series = series,
                Issues = issues,
                Release = new ReleaseInfo { Title = "x" }
            };

            return new DownloadDecision(remote, new Rejection("Unable to parse issues from release name"));
        }

        private static RssSyncCompleteEvent Event(params DownloadDecision[] rejected)
        {
            return new RssSyncCompleteEvent(new ProcessedDecisions(
                new List<DownloadDecision>(),
                new List<DownloadDecision>(),
                new List<DownloadDecision>(rejected)));
        }

        [Test]
        public void rejected_release_matching_a_series_with_no_issue_refreshes_that_series()
        {
            var series = new Series { Id = 42, Name = "Revived" };

            Subject.Handle(Event(Rejected(series, new List<Issue>())));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(c => c.Push(It.Is<RefreshSeriesCommand>(r => r.SeriesId == 42), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Once());
        }

        [Test]
        public void release_with_a_known_issue_does_not_trigger_a_refresh()
        {
            var series = new Series { Id = 42, Name = "Known" };

            Subject.Handle(Event(Rejected(series, new List<Issue> { new Issue { Id = 1 } })));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(c => c.Push(It.IsAny<RefreshSeriesCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Never());
        }

        [Test]
        public void release_with_no_series_does_not_trigger_a_refresh()
        {
            Subject.Handle(Event(Rejected(null, new List<Issue>())));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(c => c.Push(It.IsAny<RefreshSeriesCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Never());
        }

        [Test]
        public void same_series_is_refreshed_once_per_throttle_window()
        {
            var series = new Series { Id = 42, Name = "Revived" };

            Subject.Handle(Event(Rejected(series, new List<Issue>()), Rejected(series, new List<Issue>())));
            Subject.Handle(Event(Rejected(series, new List<Issue>())));

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(c => c.Push(It.Is<RefreshSeriesCommand>(r => r.SeriesId == 42), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Once());
        }
    }
}
