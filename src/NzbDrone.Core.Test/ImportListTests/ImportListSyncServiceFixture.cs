using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class ImportListSyncServiceFixture : CoreTest<ImportListSyncService>
    {
        private List<ImportListItemInfo> _importListReports;

        [SetUp]
        public void SetUp()
        {
            var importListItem1 = new ImportListItemInfo
            {
                Series = "Linkin Park"
            };

            _importListReports = new List<ImportListItemInfo> { importListItem1 };

            var mockImportList = new Mock<IImportList>();

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<ISearchForNewIssue>()
                .Setup(v => v.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new List<Issue>());

            Mocker.GetMock<ISearchForNewSeries>()
                .Setup(v => v.SearchForNewSeries(It.IsAny<string>()))
                .Returns(new List<Series>());

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = ImportListMonitorType.SpecificIssue });

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.AutomaticAddEnabled(It.IsAny<bool>()))
                .Returns(new List<IImportList> { mockImportList.Object });

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IProvideIssueInfo>()
                .Setup(v => v.GetIssueInfo(It.IsAny<string>()))
                .Returns<string>(id => Tuple.Create(
                    "edition-" + id,
                    new Issue { ForeignIssueId = "4321", Title = "Mapped Issue" },
                    new List<SeriesMetadata> { new SeriesMetadata { ForeignSeriesId = "f59c5520-5f46-4d2c-b2c4-822eabf53419", Name = "Linkin Park" } }));

            Mocker.GetMock<IAddIssueService>()
                .Setup(v => v.AddIssues(It.IsAny<List<Issue>>(), false))
                .Returns<List<Issue>, bool>((x, y) => x);

            Mocker.GetMock<IAddSeriesService>()
                .Setup(v => v.AddSeries(It.IsAny<List<Series>>(), false))
                .Returns<List<Series>, bool>((x, y) => x);
        }

        private void WithIssue()
        {
            _importListReports.First().Issue = "Meteora";
        }

        private void WithSeriesId()
        {
            _importListReports.First().ForeignSeriesId = "f59c5520-5f46-4d2c-b2c4-822eabf53419";
        }

        private void WithIssueId()
        {
            _importListReports.First().ForeignEditionId = "1234";
        }

        private void WithSecondIssue()
        {
            var importListItem2 = new ImportListItemInfo
            {
                Series = "Linkin Park",
                ForeignSeriesId = "f59c5520-5f46-4d2c-b2c4-822eabf53419",
                Issue = "Meteora 2",
                ForeignEditionId = "5678",
                ForeignIssueId = "8765"
            };
            _importListReports.Add(importListItem2);
        }

        private void WithExistingSeries()
        {
            Mocker.GetMock<ISeriesService>()
                .Setup(v => v.FindById(_importListReports.First().ForeignSeriesId))
                .Returns(new Series { Id = 1, ForeignSeriesId = _importListReports.First().ForeignSeriesId });
        }

        private void WithExistingIssue()
        {
            Mocker.GetMock<IIssueService>()
                .Setup(v => v.FindById("4321"))
                .Returns(new Issue { Id = 1, ForeignIssueId = _importListReports.First().ForeignIssueId });
        }

        private void WithExcludedSeries()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "f59c5520-5f46-4d2c-b2c4-822eabf53419"
                    }
                });
        }

        private void WithExcludedIssue()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "4321"
                    }
                });
        }

        private void WithMonitorType(ImportListMonitorType monitor)
        {
            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = monitor });
        }

        [Test]
        public void should_search_if_series_title_and_no_series_id()
        {
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewSeries>()
                .Verify(v => v.SearchForNewSeries(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_series_title_and_series_id()
        {
            WithSeriesId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewSeries>()
                .Verify(v => v.SearchForNewSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_search_if_issue_title_and_no_issue_id()
        {
            WithIssue();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewIssue>()
                .Verify(v => v.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_issue_title_and_issue_id()
        {
            WithSeriesId();
            WithIssueId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewIssue>()
                .Verify(v => v.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void should_not_search_if_all_info()
        {
            WithSeriesId();
            WithIssue();
            WithIssueId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewIssue>()
                .Verify(v => v.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never());

            Mocker.GetMock<ISearchForNewSeries>()
                .Verify(v => v.SearchForNewSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_add_if_existing_series()
        {
            WithSeriesId();
            WithExistingSeries();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddSeriesService>()
                .Verify(v => v.AddSeries(It.Is<List<Series>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_if_existing_issue()
        {
            WithIssueId();
            WithExistingIssue();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddSeriesService>()
                .Verify(v => v.AddSeries(It.Is<List<Series>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_add_if_existing_series_but_new_issue()
        {
            WithIssueId();
            WithSeriesId();
            WithExistingSeries();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 1), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, true)]
        [TestCase(ImportListMonitorType.EntireSeries, true)]
        public void should_add_if_not_existing_series(ImportListMonitorType monitor, bool expectedSeriesMonitored)
        {
            WithSeriesId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddSeriesService>()
                .Verify(v => v.AddSeries(It.Is<List<Series>>(t => t.Count == 1 && t.First().Monitored == expectedSeriesMonitored), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, true)]
        [TestCase(ImportListMonitorType.EntireSeries, true)]
        public void should_add_if_not_existing_issue(ImportListMonitorType monitor, bool expectedIssueMonitored)
        {
            WithIssueId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 1 && t.First().Monitored == expectedIssueMonitored), false));
        }

        [Test]
        public void should_not_add_series_if_excluded_series()
        {
            WithSeriesId();
            WithExcludedSeries();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddSeriesService>()
                .Verify(v => v.AddSeries(It.Is<List<Series>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_issue_if_excluded_issue()
        {
            WithIssueId();
            WithExcludedIssue();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_issue_if_excluded_series()
        {
            WithIssueId();
            WithSeriesId();
            WithExcludedSeries();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 0), false));
        }

        [TestCase(ImportListMonitorType.None, 0, false)]
        [TestCase(ImportListMonitorType.SpecificIssue, 2, true)]
        [TestCase(ImportListMonitorType.EntireSeries, 0, true)]
        public void should_add_two_issues(ImportListMonitorType monitor, int expectedIssuesMonitored, bool expectedSeriesMonitored)
        {
            WithIssue();
            WithIssueId();
            WithSecondIssue();
            WithSeriesId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddIssueService>()
                .Verify(v => v.AddIssues(It.Is<List<Issue>>(t => t.Count == 2), false));
            Mocker.GetMock<IAddSeriesService>()
                .Verify(v => v.AddSeries(It.Is<List<Series>>(t => t.Count == 1 &&
                                                                   t.First().AddOptions.IssuesToMonitor.Count == expectedIssuesMonitored &&
                                                                   t.First().Monitored == expectedSeriesMonitored), false));
        }

        [Test]
        public void should_not_fetch_if_no_lists_are_enabled()
        {
            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.AutomaticAddEnabled(It.IsAny<bool>()))
                .Returns(new List<IImportList>());

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IFetchAndParseImportList>()
                .Verify(v => v.Fetch(), Times.Never);
        }

        [Test]
        public void should_not_process_if_no_items_are_returned()
        {
            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(new List<ImportListItemInfo>());

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IImportListExclusionService>()
                .Verify(v => v.All(), Times.Never);
        }
    }
}
