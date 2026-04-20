using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class SeriesSearchServiceFixture : CoreTest<SeriesSearchService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series();

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.GetSeries(It.IsAny<int>()))
                .Returns(_series);

            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.SeriesSearch(_series.Id, false, true, false))
                .Returns(Task.FromResult(new List<DownloadDecision>()));

            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns(Task.FromResult(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>())));
        }

        [Test]
        public void should_only_include_monitored_books()
        {
            _series.Issues = new List<Issue>
            {
                new Issue { Monitored = false },
                new Issue { Monitored = true }
            };

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeriesSearch(_series.Id, false, true, false),
                    Times.Exactly(_series.Issues.Value.Count(s => s.Monitored)));
        }
    }
}
