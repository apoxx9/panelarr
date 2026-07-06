using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Test.Qualities;
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
        public void should_store_a_report_with_per_file_outcomes()
        {
            var approvedIssue = new LocalIssue { Path = $"{StagingFolder}/Saga 001.cbz" };
            var approved = new ImportDecision<LocalIssue>(approvedIssue);

            var rejectedIssue = new LocalIssue { Path = $"{StagingFolder}/Saga 002.cbz" };
            var rejectedDecision = new ImportDecision<LocalIssue>(rejectedIssue, new Rejection("Not an upgrade"));

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                  .Returns(new List<ImportDecision<LocalIssue>> { approved, rejectedDecision });

            Mocker.GetMock<IImportApprovedIssues>()
                  .Setup(s => s.Import(It.IsAny<List<ImportDecision<LocalIssue>>>(), It.IsAny<bool>(), It.IsAny<NzbDrone.Core.Download.DownloadClientItem>(), It.IsAny<ImportMode>()))
                  .Returns(new List<ImportResult> { new ImportResult(approved) });

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IStagingImportReportService>()
                  .Verify(s => s.Store(It.Is<StagingImportReport>(r =>
                      r.Files.Count == 2 &&
                      r.Files.Any(f => f.Path == approvedIssue.Path && f.Outcome == StagingImportFileOutcome.Imported) &&
                      r.Files.Any(f => f.Path == rejectedIssue.Path && f.Outcome == StagingImportFileOutcome.Rejected && f.Reasons.Single() == "Not an upgrade"))), Times.Once);
        }

        [Test]
        public void should_reject_equal_quality_duplicates_instead_of_replacing()
        {
            _series.QualityProfile = new QualityProfile { Items = QualityFixture.GetDefaultQualities() };

            var duplicateIssue = new LocalIssue
            {
                Path = $"{StagingFolder}/Saga 001.cbz",
                Quality = new QualityModel(Quality.Unknown),
                Series = _series,
                Issue = new Issue
                {
                    ComicFiles = new List<ComicFile>
                    {
                        new ComicFile { Quality = new QualityModel(Quality.Unknown) }
                    }
                }
            };

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                  .Returns(new List<ImportDecision<LocalIssue>> { new ImportDecision<LocalIssue>(duplicateIssue) });

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IImportApprovedIssues>()
                  .Verify(s => s.Import(It.Is<List<ImportDecision<LocalIssue>>>(l => l.Count == 1 && !l[0].Approved), false, null, ImportMode.Move), Times.Once);

            Mocker.GetMock<IStagingImportReportService>()
                  .Verify(s => s.Store(It.Is<StagingImportReport>(r =>
                      r.Files.Single().Outcome == StagingImportFileOutcome.Rejected &&
                      r.Files.Single().Reasons.Single().Contains("equal quality"))), Times.Once);
        }

        [Test]
        public void should_not_reject_a_quality_upgrade_of_an_existing_file()
        {
            _series.QualityProfile = new QualityProfile { Items = QualityFixture.GetDefaultQualities() };

            var upgradeIssue = new LocalIssue
            {
                Path = $"{StagingFolder}/Saga 001.cbz",
                Quality = new QualityModel(Quality.Archive),
                Series = _series,
                Issue = new Issue
                {
                    ComicFiles = new List<ComicFile>
                    {
                        new ComicFile { Quality = new QualityModel(Quality.Unknown) }
                    }
                }
            };

            Mocker.GetMock<IMakeImportDecision>()
                  .Setup(s => s.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                  .Returns(new List<ImportDecision<LocalIssue>> { new ImportDecision<LocalIssue>(upgradeIssue) });

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IImportApprovedIssues>()
                  .Verify(s => s.Import(It.Is<List<ImportDecision<LocalIssue>>>(l => l.Count == 1 && l[0].Approved), false, null, ImportMode.Move), Times.Once);
        }

        [Test]
        public void should_record_a_series_failure_in_the_report()
        {
            Mocker.GetMock<IAddSeriesService>()
                  .Setup(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()))
                  .Throws(new InvalidOperationException("metadata lookup failed"));

            Subject.Execute(GivenCommand());

            Mocker.GetMock<IStagingImportReportService>()
                  .Verify(s => s.Store(It.Is<StagingImportReport>(r =>
                      r.Files.Count == 0 &&
                      r.Errors.Count == 1 &&
                      r.Errors.Single().Contains("cv:123"))), Times.Once);

            ExceptionVerification.ExpectedErrors(1);
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
