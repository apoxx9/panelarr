using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class DeletedComicFileSpecificationFixture : CoreTest<DeletedComicFileSpecification>
    {
        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private ComicFile _firstFile;
        private ComicFile _secondFile;

        [SetUp]
        public void Setup()
        {
            _firstFile =
                new ComicFile
                {
                    Id = 1,
                    Path = "/My.Series.S01E01.mp3",
                    Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 1)),
                    DateAdded = DateTime.Now,
                    IssueId = 1
                };
            _secondFile =
                new ComicFile
                {
                    Id = 2,
                    Path = "/My.Series.S01E02.mp3",
                    Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 1)),
                    DateAdded = DateTime.Now,
                    IssueId = 2
                };

            var singleIssueList = new List<Issue> { new Issue { Id = 1 } };
            var doubleIssueList = new List<Issue>
            {
                new Issue { Id = 1 },
                new Issue { Id = 2 }
            };

            var fakeSeries = Builder<Series>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.CBZ_HD.Id })
                         .With(c => c.Path = @"C:\Music\My.Series".AsOsAgnostic())
                         .Build();

            _parseResultMulti = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = doubleIssueList
            };

            _parseResultSingle = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = singleIssueList
            };

            GivenUnmonitorDeletedTracks(true);
        }

        private void GivenUnmonitorDeletedTracks(bool enabled)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(v => v.AutoUnmonitorPreviouslyDownloadedIssues)
                  .Returns(enabled);
        }

        private void SetupMediaFile(List<ComicFile> files)
        {
            Mocker.GetMock<IMediaFileService>()
                              .Setup(v => v.GetFilesByIssue(It.IsAny<int>()))
                              .Returns(files);
        }

        private void WithExistingFile(ComicFile trackFile)
        {
            var path = trackFile.Path;

            Mocker.GetMock<IDiskProvider>()
                  .Setup(v => v.FileExists(path))
                  .Returns(true);
        }

        [Test]
        public void should_return_true_when_unmonitor_deleted_tracks_is_off()
        {
            GivenUnmonitorDeletedTracks(false);

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_searching()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, new SeriesSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_file_exists()
        {
            WithExistingFile(_firstFile);
            SetupMediaFile(new List<ComicFile> { _firstFile });

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_file_is_missing()
        {
            SetupMediaFile(new List<ComicFile> { _firstFile });
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_both_of_multiple_episode_exist()
        {
            WithExistingFile(_firstFile);
            WithExistingFile(_secondFile);
            SetupMediaFile(new List<ComicFile> { _firstFile, _secondFile });

            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_one_of_multiple_episode_is_missing()
        {
            WithExistingFile(_firstFile);
            SetupMediaFile(new List<ComicFile> { _firstFile, _secondFile });

            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }
    }
}
