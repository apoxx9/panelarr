using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class TaggedIdIdentificationFixture : CoreTest<IdentificationService>
    {
        private Issue _libraryIssue;
        private Series _series;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ITrackGroupingService>(Mocker.Resolve<TrackGroupingService>());

            _series = new Series { Id = 1, SeriesMetadataId = 10 };
            _libraryIssue = new Issue
            {
                Id = 42,
                ForeignIssueId = "cv:338482",
                Title = "A Larger World",
                IssueNumber = "16",
                SeriesMetadataId = 10,
                Series = _series
            };

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.FindById("cv:338482"))
                  .Returns(_libraryIssue);

            // unstubbed mocks return null lists, which the fallback path
            // would turn into a logged error and fail the teardown check
            Mocker.GetMock<ICandidateService>()
                  .Setup(s => s.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()))
                  .Returns(new List<CandidateEdition>());

            Mocker.GetMock<ICandidateService>()
                  .Setup(s => s.GetRemoteCandidates(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>()))
                  .Returns(new List<CandidateEdition>());
        }

        private List<LocalIssue> GivenTaggedTrack(string foreignIssueId)
        {
            return new List<LocalIssue>
            {
                new LocalIssue
                {
                    Path = @"C:\comics\The Walking Dead (2004)\The Walking Dead Vol. 16.cbz".AsOsAgnostic(),
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        SeriesTitle = "The Walking Dead",
                        Series = new List<string> { "The Walking Dead" },
                        SeriesIndex = "16",
                        ForeignIssueId = foreignIssueId
                    }
                }
            };
        }

        private ImportDecisionMakerConfig GivenConfig()
        {
            return new ImportDecisionMakerConfig
            {
                Filter = FilterFilesType.None,
                IncludeExisting = false,
                AddNewSeries = false
            };
        }

        [Test]
        public void tagged_id_in_library_should_match_exactly_without_fuzzy_matching()
        {
            var result = Subject.Identify(GivenTaggedTrack("cv:338482"), new IdentificationOverrides(), GivenConfig());

            result.Should().HaveCount(1);
            result[0].Issue.Should().Be(_libraryIssue);
            result[0].LocalIssues[0].Issue.Should().Be(_libraryIssue);

            // candidate/fuzzy machinery must not have been consulted
            Mocker.GetMock<ICandidateService>()
                  .Verify(s => s.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()), Times.Never());
        }

        [Test]
        public void explicit_issue_override_should_win_over_tagged_id()
        {
            var overrideIssue = new Issue { Id = 7, SeriesMetadataId = 10, Series = _series };
            var overrides = new IdentificationOverrides { Issue = overrideIssue, Series = _series };

            Subject.Identify(GivenTaggedTrack("cv:338482"), overrides, GivenConfig());

            Mocker.GetMock<IIssueService>()
                  .Verify(s => s.FindById(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void tagged_id_from_other_series_should_not_satisfy_series_override()
        {
            var otherSeries = new Series { Id = 2, SeriesMetadataId = 99 };
            var overrides = new IdentificationOverrides { Series = otherSeries };

            var result = Subject.Identify(GivenTaggedTrack("cv:338482"), overrides, GivenConfig());

            // falls through to the normal candidate path instead of cross-series matching
            result[0].Issue.Should().NotBe(_libraryIssue);
            Mocker.GetMock<ICandidateService>()
                  .Verify(s => s.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()), Times.AtLeastOnce());
        }

        [Test]
        public void unknown_tagged_id_should_fall_back_to_fuzzy_matching()
        {
            var result = Subject.Identify(GivenTaggedTrack("cv:999999"), new IdentificationOverrides(), GivenConfig());

            result[0].Issue.Should().BeNull();
            Mocker.GetMock<ICandidateService>()
                  .Verify(s => s.GetDbCandidatesFromTags(It.IsAny<LocalEdition>(), It.IsAny<IdentificationOverrides>(), It.IsAny<bool>()), Times.AtLeastOnce());
        }
    }
}
