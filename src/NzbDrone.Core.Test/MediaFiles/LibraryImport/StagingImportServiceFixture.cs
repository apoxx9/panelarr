using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.LibraryImport
{
    [TestFixture]
    public class StagingImportServiceFixture : CoreTest<StagingImportService>
    {
        private const string RootPath = "/comics";
        private const string StagingFolder = "/staging/Saga (2012)";

        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
            {
                Id = 5,
                Metadata = new SeriesMetadata { Id = 1, Name = "Saga", ForeignSeriesId = "cv:123" }
            };

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.All())
                  .Returns(new List<RootFolder> { new RootFolder { Id = 1, Path = RootPath } });

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:123"))
                  .Returns((Series)null);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(_series.Id))
                  .Returns(_series);

            Mocker.GetMock<IAddSeriesService>()
                  .Setup(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()))
                  .Returns(_series);

            GivenStagingFiles(2);

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                  .Returns(new List<ImportDecision<LocalIssue>>());

            Mocker.GetMock<IImportApprovedIssues>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision<LocalIssue>>>(), It.IsAny<bool>(), It.IsAny<NzbDrone.Core.Download.DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult>());
        }

        private void GivenStagingFiles(int count)
        {
            var files = Enumerable.Range(1, count)
                .Select(i =>
                {
                    var file = new Mock<IFileInfo>();
                    file.SetupGet(f => f.FullName).Returns($"{StagingFolder}/Saga {i:000}.cbz");
                    return file.Object;
                })
                .ToArray();

            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetComicFiles(StagingFolder, false))
                  .Returns(files);
        }

        private StagingImportCommand GivenCommand(bool keepSourceFiles = false)
        {
            return new StagingImportCommand
            {
                Series = new List<LibraryImportSeries>
                {
                    new LibraryImportSeries { ForeignSeriesId = "cv:123", Folder = StagingFolder }
                },
                QualityProfileId = 1,
                Monitored = true,
                RootFolderPath = RootPath,
                KeepSourceFiles = keepSourceFiles
            };
        }

        [Test]
        public void should_throw_when_target_is_not_a_configured_root_folder()
        {
            var command = GivenCommand();
            command.RootFolderPath = "/not-a-root";

            Assert.Throws<ArgumentException>(() => Subject.Execute(command));
        }

        [Test]
        public void should_add_missing_series_without_refresh_then_refresh_synchronously()
        {
            Subject.Execute(GivenCommand());

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.Is<Series>(x => x.RootFolderPath == RootPath && x.Metadata.Value.ForeignSeriesId == "cv:123"), false), Times.Once);

            Mocker.GetMock<IRefreshSeriesService>()
                  .Verify(s => s.RefreshSeries(It.Is<List<int>>(ids => ids.Single() == _series.Id), true, CommandTrigger.Manual), Times.Once);
        }

        [Test]
        public void should_reuse_existing_series_without_adding_or_refreshing()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:123"))
                  .Returns(_series);

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IAddSeriesService>()
                  .Verify(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()), Times.Never);

            Mocker.GetMock<IRefreshSeriesService>()
                  .Verify(s => s.RefreshSeries(It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<CommandTrigger>()), Times.Never);
        }

        [Test]
        public void should_import_with_move_by_default()
        {
            Subject.Execute(GivenCommand());

            Mocker.GetMock<IImportApprovedIssues>()
                  .Verify(s => s.Import(It.IsAny<List<ImportDecision<LocalIssue>>>(), false, null, ImportMode.Move), Times.Once);
        }

        [Test]
        public void should_import_with_copy_when_keeping_source_files()
        {
            Subject.Execute(GivenCommand(keepSourceFiles: true));

            Mocker.GetMock<IImportApprovedIssues>()
                  .Verify(s => s.Import(It.IsAny<List<ImportDecision<LocalIssue>>>(), false, null, ImportMode.Copy), Times.Once);
        }

        [Test]
        public void should_force_the_series_when_building_decisions()
        {
            Subject.Execute(GivenCommand());

            Mocker.GetMock<IMakeImportDecision>()
                  .Verify(s => s.GetImportDecisions(It.Is<List<IFileInfo>>(f => f.Count == 2), It.Is<IdentificationOverrides>(o => o.Series == _series), null, It.Is<ImportDecisionMakerConfig>(c => c.NewDownload && !c.AddNewSeries && !c.IncludeExisting)), Times.Once);
        }

        [Test]
        public void should_not_import_when_staging_folder_has_no_comic_files()
        {
            Mocker.GetMock<IDiskScanService>()
                  .Setup(s => s.GetComicFiles(StagingFolder, false))
                  .Returns(Array.Empty<IFileInfo>());

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IImportApprovedIssues>()
                  .Verify(s => s.Import(It.IsAny<List<ImportDecision<LocalIssue>>>(), It.IsAny<bool>(), It.IsAny<NzbDrone.Core.Download.DownloadClientItem>(), It.IsAny<ImportMode>()), Times.Never);

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
