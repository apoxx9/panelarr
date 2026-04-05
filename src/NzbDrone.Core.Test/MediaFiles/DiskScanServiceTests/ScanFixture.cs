using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.DiskScanServiceTests
{
    [TestFixture]
    public class ScanFixture : FileSystemTest<DiskScanService>
    {
        private Series _series;
        private string _rootFolder;
        private string _otherSeriesFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = @"C:\Test\Music".AsOsAgnostic();
            _otherSeriesFolder = @"C:\Test\Music\OtherSeries".AsOsAgnostic();
            var seriesFolder = @"C:\Test\Music\Series".AsOsAgnostic();

            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = seriesFolder)
                                     .Build();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder { Path = _rootFolder });

            Mocker.GetMock<ISeriesService>()
                .Setup(s => s.GetSeries(It.IsAny<List<int>>()))
                .Returns(new List<Series>());

            Mocker.GetMock<IMakeImportDecision>()
                .Setup(v => v.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                .Returns(new List<ImportDecision<LocalIssue>>());

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesBySeries(It.IsAny<int>()))
                .Returns(new List<ComicFile>());

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesWithBasePath(It.IsAny<string>()))
                .Returns(new List<ComicFile>());

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.FilterUnchangedFiles(It.IsAny<List<IFileInfo>>(), It.IsAny<FilterFilesType>()))
                .Returns((List<IFileInfo> files, FilterFilesType filter) => files);
        }

        private void GivenRootFolder(params string[] subfolders)
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(_rootFolder))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetDirectories(_rootFolder))
                .Returns(subfolders);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderEmpty(_rootFolder))
                .Returns(subfolders.Empty());

            FileSystem.AddDirectory(_rootFolder);

            foreach (var folder in subfolders)
            {
                Mocker.GetMock<IDiskProvider>()
                    .Setup(s => s.FolderExists(folder))
                    .Returns(true);

                FileSystem.AddDirectory(folder);
            }
        }

        private void GivenSeriesFolder()
        {
            GivenRootFolder(_series.Path);
        }

        private void GivenFiles(IEnumerable<string> files, DateTimeOffset? lastWrite = null)
        {
            if (lastWrite == null)
            {
                TestLogger.Debug("Using default lastWrite");
                lastWrite = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            foreach (var file in files)
            {
                FileSystem.AddFile(file, new MockFileData(string.Empty) { LastWriteTime = lastWrite.Value });
            }

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.GetFileInfos(It.IsAny<string>(), true))
                .Returns(files.Select(x => DiskProvider.GetFileInfo(x)).ToList());
        }

        private void GivenKnownFiles(IEnumerable<string> files, DateTimeOffset? lastWrite = null)
        {
            if (lastWrite == null)
            {
                TestLogger.Debug("Using default lastWrite");
                lastWrite = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            Mocker.GetMock<IMediaFileService>()
                .Setup(x => x.GetFilesWithBasePath(_series.Path))
                .Returns(files.Select(x => new ComicFile
                {
                    Path = x,
                    Modified = lastWrite.Value.UtcDateTime
                }).ToList());
        }

        [Test]
        public void should_not_scan_if_root_folder_does_not_exist()
        {
            Subject.Scan(new List<string> { _series.Path });

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.FolderExists(_series.Path), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Never());
        }

        [Test]
        public void should_not_scan_if_root_folder_is_empty()
        {
            GivenRootFolder();

            Subject.Scan(new List<string> { _series.Path });

            ExceptionVerification.ExpectedWarns(1);

            Mocker.GetMock<IDiskProvider>()
                  .Verify(v => v.GetFiles(_series.Path, true), Times.Never());

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Never());
        }

        [Test]
        public void should_clean_if_folder_does_not_exist()
        {
            GivenRootFolder(_otherSeriesFolder);

            Subject.Scan(new List<string> { _series.Path });

            DiskProvider.FolderExists(_series.Path).Should().BeFalse();

            Mocker.GetMock<IMediaFileTableCleanupService>()
                  .Verify(v => v.Clean(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once());
        }

        [Test]
        public void should_find_files_at_root_of_series_folder()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "file1.cbr"),
                           Path.Combine(_series.Path, "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 2), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_extras_subfolder()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "EXTRAS", "file1.cbr"),
                           Path.Combine(_series.Path, "Extras", "file2.cbr"),
                           Path.Combine(_series.Path, "EXTRAs", "file3.cbr"),
                           Path.Combine(_series.Path, "ExTrAs", "file4.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IDiskProvider>()
                .Verify(v => v.GetFileInfos(It.IsAny<string>(), It.IsAny<bool>()), Times.Once());

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_AppleDouble_subfolder()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, ".AppleDouble", "file1.cbr"),
                           Path.Combine(_series.Path, ".appledouble", "file2.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_scan_extras_author_and_subfolders()
        {
            _series.Path = @"C:\Test\Music\Extras".AsOsAgnostic();

            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "Extras", "file1.cbr"),
                           Path.Combine(_series.Path, ".AppleDouble", "file2.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e02.cbr"),
                           Path.Combine(_series.Path, "Season 2", "s02e01.cbr"),
                           Path.Combine(_series.Path, "Season 2", "s02e02.cbr"),
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 4), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_scan_files_that_start_with_period()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "Issue 1", ".t01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolders_that_start_with_period()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, ".@__thumb", "file1.cbr"),
                           Path.Combine(_series.Path, ".@__THUMB", "file2.cbr"),
                           Path.Combine(_series.Path, ".hidden", "file2.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_subfolder_of_season_folder_that_starts_with_a_period()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "Season 1", ".@__thumb", "file1.cbr"),
                           Path.Combine(_series.Path, "Season 1", ".@__THUMB", "file2.cbr"),
                           Path.Combine(_series.Path, "Season 1", ".hidden", "file2.cbr"),
                           Path.Combine(_series.Path, "Season 1", ".AppleDouble", "s01e01.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_Synology_eaDir()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "@eaDir", "file1.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_not_scan_thumb_folder()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, ".@__thumb", "file1.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_scan_dotHack_folder()
        {
            _series.Path = @"C:\Test\Music\.hack".AsOsAgnostic();

            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                           Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 2), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        [Test]
        public void should_exclude_osx_metadata_files()
        {
            GivenSeriesFolder();

            GivenFiles(new List<string>
                       {
                           Path.Combine(_series.Path, ".DS_STORE"),
                           Path.Combine(_series.Path, "._24 The Status Quo Combustion.cbr"),
                           Path.Combine(_series.Path, "24 The Status Quo Combustion.cbr")
                       });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMakeImportDecision>()
                .Verify(v => v.GetImportDecisions(It.Is<List<IFileInfo>>(l => l.Count == 1), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()), Times.Once());
        }

        private void GivenRejections()
        {
            Mocker.GetMock<IMakeImportDecision>()
                .Setup(x => x.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                .Returns((List<IFileInfo> fileList, IdentificationOverrides idOverrides, ImportDecisionMakerInfo idInfo, ImportDecisionMakerConfig idConfig) =>
                          fileList.Select(x => new LocalIssue
                          {
                              Series = _series,
                              Path = x.FullName,
                              Modified = x.LastWriteTimeUtc,
                              FileTrackInfo = new ParsedTrackInfo()
                          })
                          .Select(x => new ImportDecision<LocalIssue>(x, new Rejection("Reject")))
                          .ToList());
        }

        [Test]
        public void should_insert_new_unmatched_files_when_all_new()
        {
            GivenSeriesFolder();

            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
            };

            GivenFiles(files);
            GivenKnownFiles(new List<string>());
            GivenRejections();

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.AddMany(It.Is<List<ComicFile>>(l => l.Select(t => t.Path).SequenceEqual(files))),
                        Times.Once());
        }

        [Test]
        public void should_insert_new_unmatched_files_when_some_known()
        {
            GivenSeriesFolder();

            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
            };

            GivenFiles(files);
            GivenKnownFiles(files.GetRange(1, 1));
            GivenRejections();

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.AddMany(It.Is<List<ComicFile>>(l => l.Select(t => t.Path).SequenceEqual(files.GetRange(0, 1)))),
                        Times.Once());
        }

        [Test]
        public void should_not_insert_files_when_all_known()
        {
            GivenSeriesFolder();

            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
            };

            GivenFiles(files);
            GivenKnownFiles(files);
            GivenRejections();

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.AddMany(It.Is<List<ComicFile>>(l => l.Count == 0)),
                        Times.Once());

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.AddMany(It.Is<List<ComicFile>>(l => l.Count > 0)),
                        Times.Never());
        }

        [Test]
        public void should_not_update_info_for_unchanged_known_files()
        {
            GivenSeriesFolder();

            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
            };

            GivenFiles(files);
            GivenKnownFiles(files);
            GivenRejections();

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.Update(It.Is<List<ComicFile>>(l => l.Count == 0)),
                        Times.Once());

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.Update(It.Is<List<ComicFile>>(l => l.Count > 0)),
                        Times.Never());
        }

        [Test]
        public void should_update_info_for_changed_known_files()
        {
            GivenSeriesFolder();

            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
                Path.Combine(_series.Path, "Season 1", "s01e01.cbr")
            };

            GivenFiles(files, new DateTime(2019, 2, 1));
            GivenKnownFiles(files);
            GivenRejections();

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.Update(It.Is<List<ComicFile>>(l => l.Count == 2)),
                        Times.Once());
        }

        [Test]
        public void should_update_fields_for_updated_files()
        {
            var files = new List<string>
            {
                Path.Combine(_series.Path, "Season 1", "file1.cbr"),
            };

            GivenKnownFiles(files);

            FileSystem.AddFile(files[0], new MockFileData("".PadRight(100)) { LastWriteTime = new DateTime(2019, 2, 1) });

            var localTrack = Builder<LocalIssue>.CreateNew()
                .With(x => x.Path = files[0])
                .With(x => x.Modified = new DateTime(2019, 2, 1))
                .With(x => x.Size = 100)
                .With(x => x.Quality = new QualityModel(Quality.CBR))
                .With(x => x.FileTrackInfo = new ParsedTrackInfo
                {
                    MediaInfo = Builder<MediaInfoModel>.CreateNew().Build()
                })
                .Build();

            Mocker.GetMock<IMakeImportDecision>()
                .Setup(x => x.GetImportDecisions(It.IsAny<List<IFileInfo>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerInfo>(), It.IsAny<ImportDecisionMakerConfig>()))
                .Returns(new List<ImportDecision<LocalIssue>> { new ImportDecision<LocalIssue>(localTrack, new Rejection("Reject")) });

            Subject.Scan(new List<string> { _series.Path });

            Mocker.GetMock<IMediaFileService>()
                .Verify(x => x.Update(It.Is<List<ComicFile>>(
                                          l => l.Count == 1 &&
                                          l[0].Path == localTrack.Path &&
                                          l[0].Modified == localTrack.Modified &&
                                          l[0].Size == localTrack.Size &&
                                          l[0].Quality.Equals(localTrack.Quality) &&
                                          l[0].MediaInfo.AudioFormat == localTrack.FileTrackInfo.MediaInfo.AudioFormat)),
                        Times.Once());
        }
    }
}
