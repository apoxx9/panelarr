using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.QueueTests
{
    [TestFixture]
    public class QueueServiceFixture : CoreTest<QueueService>
    {
        private List<TrackedDownload> _trackedDownloads;

        [SetUp]
        public void SetUp()
        {
            var downloadClientInfo = Builder<DownloadClientItemClientInfo>.CreateNew().Build();

            var downloadItem = Builder<NzbDrone.Core.Download.DownloadClientItem>.CreateNew()
                .With(v => v.RemainingTime = TimeSpan.FromSeconds(10))
                .With(v => v.DownloadClientInfo = downloadClientInfo)
                .Build();

            var series = Builder<Series>.CreateNew()
                .Build();

            var issues = Builder<Issue>.CreateListOfSize(3)
                .All()
                .With(e => e.SeriesId = series.Id)
                .Build();

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(r => r.Series = series)
                .With(r => r.Issues = new List<Issue>(issues))
                .With(r => r.ParsedIssueInfo = new ParsedIssueInfo())
                .Build();

            _trackedDownloads = Builder<TrackedDownload>.CreateListOfSize(1)
                .All()
                .With(v => v.IsTrackable = true)
                .With(v => v.DownloadItem = downloadItem)
                .With(v => v.RemoteIssue = remoteIssue)
                .Build()
                .ToList();

            var historyItem = Builder<EntityHistory>.CreateNew()
                .Build();

            Mocker.GetMock<IHistoryService>()
                .Setup(c => c.Find(It.IsAny<string>(), EntityHistoryEventType.Grabbed)).Returns(
                    new List<EntityHistory> { historyItem });
        }

        [Test]
        public void queue_items_should_have_id()
        {
            Subject.Handle(new TrackedDownloadRefreshedEvent(_trackedDownloads));

            var queue = Subject.GetQueue();

            queue.Should().HaveCount(3);

            queue.All(v => v.Id > 0).Should().BeTrue();

            var distinct = queue.Select(v => v.Id).Distinct().ToArray();

            distinct.Should().HaveCount(3);
        }
    }
}
