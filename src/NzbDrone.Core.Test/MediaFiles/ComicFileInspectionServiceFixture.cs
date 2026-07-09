using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ComicFileInspectionServiceFixture : CoreTest<ComicFileInspectionService>
    {
        private ComicFile _file;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            _file = new ComicFile
            {
                Id = 1,
                IssueId = 10,
                Path = @"C:\comics\Publisher\Series (2026)\Series 001 (2026).cbz".AsOsAgnostic(),
                ComicFormat = ComicFormat.CBZ
            };

            _issue = new Issue { Id = 10 };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesPendingInspection())
                  .Returns(new List<ComicFile> { _file });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssues(It.IsAny<List<int>>()))
                  .Returns(new List<Issue> { _issue });

            GivenInspectionResult(new ArchiveInspectionResult
            {
                PageCount = 22,
                ImageCount = 22,
                ImageQualityScore = 0.9f
            });
        }

        private void GivenInspectionResult(ArchiveInspectionResult result)
        {
            Mocker.GetMock<IArchiveInspector>()
                  .Setup(s => s.Inspect(_file.Path, _file.ComicFormat))
                  .Returns(result);
        }

        [Test]
        public void should_inspect_pending_files_and_persist_results()
        {
            Subject.Execute(new InspectComicFilesCommand());

            _file.ImageCount.Should().Be(22);
            _file.ImageQualityScore.Should().Be(0.9f);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.Is<List<ComicFile>>(l => l.Contains(_file))), Times.Once());
        }

        [Test]
        public void should_populate_credits_from_comicinfo_in_same_pass()
        {
            GivenInspectionResult(new ArchiveInspectionResult
            {
                ImageCount = 22,
                ImageQualityScore = 0.9f,
                ComicInfoXml = "<ComicInfo/>"
            });

            var credits = new List<Credit> { new Credit { PersonName = "Jeff Lemire", Role = "Writer" } };

            Mocker.GetMock<ICreditExtractorService>()
                  .Setup(s => s.ParseCredits("<ComicInfo/>"))
                  .Returns(credits);

            Subject.Execute(new InspectComicFilesCommand());

            _issue.Credits.Should().BeSameAs(credits);

            Mocker.GetMock<IIssueService>()
                  .Verify(s => s.UpdateMany(It.Is<List<Issue>>(l => l.Contains(_issue))), Times.Once());
        }

        [Test]
        public void should_not_overwrite_existing_credits()
        {
            _issue.Credits = new List<Credit> { new Credit { PersonName = "Existing", Role = "Writer" } };

            GivenInspectionResult(new ArchiveInspectionResult
            {
                ImageCount = 22,
                ComicInfoXml = "<ComicInfo/>"
            });

            Subject.Execute(new InspectComicFilesCommand());

            Mocker.GetMock<ICreditExtractorService>()
                  .Verify(s => s.ParseCredits(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<IIssueService>()
                  .Verify(s => s.UpdateMany(It.IsAny<List<Issue>>()), Times.Never());
        }

        [Test]
        public void should_attempt_failed_inspections_only_once_per_run()
        {
            // A failed inspection leaves ImageCount at 0, so the file stays in
            // the pending query — the run must still terminate
            GivenInspectionResult(new ArchiveInspectionResult { Error = "Rar signature not found" });

            Subject.Execute(new InspectComicFilesCommand());

            Mocker.GetMock<IArchiveInspector>()
                  .Verify(s => s.Inspect(_file.Path, _file.ComicFormat), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void added_mapped_file_should_queue_inspection()
        {
            Subject.Handle(new ComicFileAddedEvent(_file));

            VerifyCommandQueued(Times.Once());
        }

        [Test]
        public void added_unmapped_file_should_not_queue_inspection()
        {
            _file.IssueId = 0;

            Subject.Handle(new ComicFileAddedEvent(_file));

            VerifyCommandQueued(Times.Never());
        }

        [Test]
        public void series_scanned_should_queue_inspection_when_files_pending()
        {
            Subject.Handle(new SeriesScannedEvent(new Series()));

            VerifyCommandQueued(Times.Once());
        }

        [Test]
        public void series_scanned_should_not_queue_inspection_when_nothing_pending()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesPendingInspection())
                  .Returns(new List<ComicFile>());

            Subject.Handle(new SeriesScannedEvent(new Series()));

            VerifyCommandQueued(Times.Never());
        }

        private void VerifyCommandQueued(Times times)
        {
            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.Push(It.IsAny<InspectComicFilesCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), times);
        }
    }
}
