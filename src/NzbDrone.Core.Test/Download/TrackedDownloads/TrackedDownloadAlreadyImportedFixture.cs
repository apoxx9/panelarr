using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class TrackedDownloadAlreadyImportedFixture : CoreTest<TrackedDownloadAlreadyImported>
    {
        private List<Issue> _issues;
        private TrackedDownload _trackedDownload;
        private List<EntityHistory> _historyItems;

        [SetUp]
        public void Setup()
        {
            _issues = new List<Issue>();

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                      .With(r => r.Issues = _issues)
                                                      .Build();

            var downloadItem = Builder<DownloadClientItem>.CreateNew().Build();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                                                       .With(t => t.RemoteIssue = remoteIssue)
                                                       .With(t => t.DownloadItem = downloadItem)
                                                       .Build();

            _historyItems = new List<EntityHistory>();
        }

        public void GivenIssues(int count)
        {
            _issues.AddRange(Builder<Issue>.CreateListOfSize(count)
                                               .BuildList());
        }

        public void GivenHistoryForIssue(Issue issue, params EntityHistoryEventType[] eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                _historyItems.Add(
                    Builder<EntityHistory>.CreateNew()
                                            .With(h => h.IssueId = issue.Id)
                                            .With(h => h.EventType = eventType)
                                            .Build());
            }
        }

        [Test]
        public void should_return_false_if_there_is_no_history()
        {
            GivenIssues(1);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_single_issue_download_is_not_imported()
        {
            GivenIssues(1);

            GivenHistoryForIssue(_issues[0], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_false_if_no_issue_in_multi_issue_download_is_imported()
        {
            GivenIssues(2);

            GivenHistoryForIssue(_issues[0], EntityHistoryEventType.Grabbed);
            GivenHistoryForIssue(_issues[1], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_should_return_false_if_only_one_issue_in_multi_issue_download_is_imported()
        {
            GivenIssues(2);

            GivenHistoryForIssue(_issues[0], EntityHistoryEventType.ComicFileImported, EntityHistoryEventType.Grabbed);
            GivenHistoryForIssue(_issues[1], EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeFalse();
        }

        [Test]
        public void should_return_true_if_single_issue_download_is_imported()
        {
            GivenIssues(1);

            GivenHistoryForIssue(_issues[0], EntityHistoryEventType.ComicFileImported, EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeTrue();
        }

        [Test]
        public void should_return_true_if_multi_issue_download_is_imported()
        {
            GivenIssues(2);

            GivenHistoryForIssue(_issues[0], EntityHistoryEventType.ComicFileImported, EntityHistoryEventType.Grabbed);
            GivenHistoryForIssue(_issues[1], EntityHistoryEventType.ComicFileImported, EntityHistoryEventType.Grabbed);

            Subject.IsImported(_trackedDownload, _historyItems)
                   .Should()
                   .BeTrue();
        }
    }
}
