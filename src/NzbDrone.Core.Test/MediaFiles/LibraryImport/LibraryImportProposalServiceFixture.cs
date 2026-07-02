using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.LibraryImport
{
    [TestFixture]
    public class LibraryImportProposalServiceFixture : CoreTest<LibraryImportProposalService>
    {
        private string _rootPath;
        private string _twdFolder;

        [SetUp]
        public void Setup()
        {
            _rootPath = @"C:\comics".AsOsAgnostic();
            _twdFolder = Path.Combine(_rootPath, "The Walking Dead (2004)");

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.Get(1))
                  .Returns(new RootFolder { Id = 1, Path = _rootPath });

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById(It.IsAny<string>()))
                  .Returns((Series)null);

            // default: no tags readable
            Mocker.GetMock<IMetadataTagService>()
                  .Setup(s => s.ReadTags(It.IsAny<System.IO.Abstractions.IFileInfo>()))
                  .Returns(new ParsedFileTagInfo());

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetFileInfo(It.IsAny<string>()))
                  .Returns((System.IO.Abstractions.IFileInfo)null);
        }

        private void GivenUnmappedFiles(string folder, int count)
        {
            var files = Enumerable.Range(1, count)
                .Select(i => new ComicFile { Id = i, IssueId = 0, Path = Path.Combine(folder, $"Issue {i:000}.cbz") })
                .ToList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetUnmappedFiles())
                  .Returns(files);
        }

        private void GivenCvInfo(string folder, string url)
        {
            var cvInfoPath = Path.Combine(folder, "cvinfo");

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(cvInfoPath))
                  .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.ReadAllText(cvInfoPath))
                  .Returns(url);
        }

        private void GivenTags(ParsedFileTagInfo tags)
        {
            Mocker.GetMock<IMetadataTagService>()
                  .Setup(s => s.ReadTags(It.IsAny<System.IO.Abstractions.IFileInfo>()))
                  .Returns(tags);
        }

        [Test]
        public void cvinfo_should_yield_exact_proposal_without_provider_calls()
        {
            GivenUnmappedFiles(_twdFolder, 31);
            GivenCvInfo(_twdFolder, "https://comicvine.gamespot.com/the-walking-dead/4050-30345/");
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", Year = 2004 });

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].ForeignSeriesId.Should().Be("cv:30345");
            proposals[0].Confidence.Should().Be(ProposalConfidence.Exact);
            proposals[0].IdSource.Should().Be("cvinfo");
            proposals[0].FileCount.Should().Be(31);
            proposals[0].Name.Should().Be("The Walking Dead");

            Mocker.GetMock<IProvideIssueInfo>()
                  .Verify(s => s.GetIssueInfo(It.IsAny<string>()), Times.Never());
            Mocker.GetMock<ISearchForNewSeries>()
                  .Verify(s => s.SearchForNewSeries(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void tagged_issue_id_should_resolve_series_when_no_cvinfo()
        {
            GivenUnmappedFiles(_twdFolder, 5);
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", ForeignIssueId = "cv:338482", Year = 2012 });

            Mocker.GetMock<IProvideIssueInfo>()
                  .Setup(s => s.GetIssueInfo("cv:338482"))
                  .Returns(Tuple.Create("cv:30345", new Issue(), new List<SeriesMetadata> { new SeriesMetadata { Name = "The Walking Dead", Year = 2004 } }));

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].ForeignSeriesId.Should().Be("cv:30345");
            proposals[0].Confidence.Should().Be(ProposalConfidence.Exact);
            proposals[0].IdSource.Should().Be("file tags");
            proposals[0].Year.Should().Be(2004);
        }

        [Test]
        public void unresolved_tagged_id_echo_should_fall_back_to_name_search()
        {
            GivenUnmappedFiles(_twdFolder, 5);
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", ForeignIssueId = "cv:692016", Year = 2004 });

            // The proxy echoes the issue id back when the provider payload
            // carries no parent series — not a real series id
            Mocker.GetMock<IProvideIssueInfo>()
                  .Setup(s => s.GetIssueInfo("cv:692016"))
                  .Returns(Tuple.Create("cv:692016", new Issue(), new List<SeriesMetadata> { new SeriesMetadata { ForeignSeriesId = "cv:692016" } }));

            Mocker.GetMock<ISearchForNewSeries>()
                  .Setup(s => s.SearchForNewSeries(It.IsAny<string>()))
                  .Returns(new List<Series>
                  {
                      new Series { Metadata = new SeriesMetadata { ForeignSeriesId = "cv:30345", Name = "The Walking Dead", Year = 2004 } }
                  });

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].ForeignSeriesId.Should().Be("cv:30345");
            proposals[0].Confidence.Should().Be(ProposalConfidence.Probable);
        }

        [Test]
        public void display_year_should_come_from_folder_not_sampled_issue_tags()
        {
            GivenUnmappedFiles(_twdFolder, 5);

            // The sampled file carries a later issue's cover year
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", Year = 2015 });
            GivenCvInfo(_twdFolder, "https://comicvine.gamespot.com/the-walking-dead/4050-30345/");

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].Year.Should().Be(2004);
        }

        [Test]
        public void name_search_should_yield_probable_proposal()
        {
            GivenUnmappedFiles(_twdFolder, 5);
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", Year = 2004 });

            Mocker.GetMock<ISearchForNewSeries>()
                  .Setup(s => s.SearchForNewSeries("The Walking Dead 2004"))
                  .Returns(new List<Series>
                  {
                      new Series { Metadata = new SeriesMetadata { ForeignSeriesId = "cv:30345", Name = "The Walking Dead", Year = 2004 } }
                  });

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].Confidence.Should().Be(ProposalConfidence.Probable);
            proposals[0].IdSource.Should().Be("name search");
        }

        [Test]
        public void stale_tagged_id_should_fall_back_to_name_search()
        {
            GivenUnmappedFiles(_twdFolder, 5);
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead", ForeignIssueId = "cv:999", Year = 2004 });

            Mocker.GetMock<IProvideIssueInfo>()
                  .Setup(s => s.GetIssueInfo("cv:999"))
                  .Throws(new Exception("410 gone"));

            Mocker.GetMock<ISearchForNewSeries>()
                  .Setup(s => s.SearchForNewSeries(It.IsAny<string>()))
                  .Returns(new List<Series>
                  {
                      new Series { Metadata = new SeriesMetadata { ForeignSeriesId = "cv:30345", Name = "The Walking Dead", Year = 2004 } }
                  });

            var proposals = Subject.GetProposals(1);

            proposals.Should().HaveCount(1);
            proposals[0].Confidence.Should().Be(ProposalConfidence.Probable);
            proposals[0].IdSource.Should().Be("name search (stale id)");
        }

        [Test]
        public void folder_without_usable_metadata_should_yield_no_proposal()
        {
            GivenUnmappedFiles(Path.Combine(_rootPath, "0xDEAD"), 2);

            Subject.GetProposals(1).Should().BeEmpty();
        }

        [Test]
        public void existing_series_should_be_flagged()
        {
            GivenUnmappedFiles(_twdFolder, 3);
            GivenCvInfo(_twdFolder, "https://comicvine.gamespot.com/the-walking-dead/4050-30345/");
            GivenTags(new ParsedFileTagInfo { SeriesTitle = "The Walking Dead" });

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindById("cv:30345"))
                  .Returns(new Series { Id = 6 });

            Subject.GetProposals(1).Single().ExistingSeriesId.Should().Be(6);
        }

        [Test]
        public void files_outside_the_root_folder_should_be_ignored()
        {
            var files = new List<ComicFile>
            {
                new ComicFile { Id = 1, IssueId = 0, Path = @"D:\elsewhere\Comic 001.cbz".AsOsAgnostic() }
            };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetUnmappedFiles())
                  .Returns(files);

            Subject.GetProposals(1).Should().BeEmpty();
        }
    }
}
