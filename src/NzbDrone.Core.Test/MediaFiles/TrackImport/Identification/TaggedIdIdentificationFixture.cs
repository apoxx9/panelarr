using System.Collections.Generic;
using System.Linq;
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
        public void tag_id_duplicated_across_many_files_should_be_distrusted()
        {
            // Tagger accident: one issue's ComicInfo stamped into a whole
            // folder (observed live: 18 files all tagged as issue #2). The
            // shared id must not exact-match every file onto the same issue.
            var tracks = new List<LocalIssue>();
            for (var i = 1; i <= 3; i++)
            {
                tracks.Add(new LocalIssue
                {
                    Path = $@"C:\comics\Saga (2012)\Saga #00{i} (2012).cbz".AsOsAgnostic(),
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        SeriesTitle = "Saga",
                        Series = new List<string> { "Saga" },
                        SeriesIndex = "2",
                        IssueTitle = "The Same Wrong Title",
                        ForeignIssueId = "cv:338482"
                    }
                });
            }

            Subject.Identify(tracks, new IdentificationOverrides(), GivenConfig());

            tracks.Should().OnlyContain(t => t.FileTagInfo.ForeignIssueId == null);
            tracks.Should().OnlyContain(t => t.FileTagInfo.IssueTitle == null);
            tracks.Select(t => t.FileTagInfo.SeriesIndex).Should().BeEquivalentTo(new[] { "1", "2", "3" });

            Mocker.GetMock<IIssueService>()
                  .Verify(s => s.FindById(It.IsAny<string>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void duplicated_title_and_number_without_ids_should_be_distrusted()
        {
            // The same tagger accident can stamp title + number but no id
            // (observed live: 6 files all tagged "Part Two"/#2, only the real
            // #2 carrying the ComicVine id). The id heuristic can't see it -
            // the title + number duplication must.
            var tracks = new List<LocalIssue>();
            for (var i = 1; i <= 4; i++)
            {
                tracks.Add(new LocalIssue
                {
                    Path = $@"C:\comics\Angel & Faith Season 10 (2014)\Angel & Faith Season 10 #00{i} (2014).cbz".AsOsAgnostic(),
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        SeriesTitle = "Angel & Faith Season 10",
                        Series = new List<string> { "Angel & Faith Season 10" },
                        SeriesIndex = "2",
                        IssueTitle = "The Same Wrong Title",
                        ForeignIssueId = i == 2 ? "cv:338482" : null
                    }
                });
            }

            Subject.Identify(tracks, new IdentificationOverrides(), GivenConfig());

            tracks.Should().OnlyContain(t => t.FileTagInfo.ForeignIssueId == null);
            tracks.Should().OnlyContain(t => t.FileTagInfo.IssueTitle == null);
            tracks.Select(t => t.FileTagInfo.SeriesIndex).Should().BeEquivalentTo(new[] { "1", "2", "3", "4" });

            Mocker.GetMock<IIssueService>()
                  .Verify(s => s.FindById(It.IsAny<string>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void distinct_titles_and_numbers_should_stay_trusted()
        {
            var tracks = new List<LocalIssue>();
            for (var i = 1; i <= 4; i++)
            {
                tracks.Add(new LocalIssue
                {
                    Path = $@"C:\comics\Saga (2012)\Saga #00{i} (2012).cbz".AsOsAgnostic(),
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        SeriesTitle = "Saga",
                        Series = new List<string> { "Saga" },
                        SeriesIndex = i.ToString(),
                        IssueTitle = $"Chapter {i}"
                    }
                });
            }

            Subject.Identify(tracks, new IdentificationOverrides(), GivenConfig());

            tracks.Select(t => t.FileTagInfo.IssueTitle).Should().BeEquivalentTo(new[] { "Chapter 1", "Chapter 2", "Chapter 3", "Chapter 4" });
        }

        [Test]
        public void title_and_number_shared_by_two_files_should_stay_trusted()
        {
            // A pair is a legitimate duplicate copy (e.g. .cbr + .cbz)
            var tracks = new List<LocalIssue>();
            for (var i = 0; i < 2; i++)
            {
                tracks.Add(new LocalIssue
                {
                    Path = $@"C:\comics\Saga (2012)\Saga #016 copy{i}.cbz".AsOsAgnostic(),
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        SeriesTitle = "Saga",
                        Series = new List<string> { "Saga" },
                        SeriesIndex = "16",
                        IssueTitle = "A Larger World"
                    }
                });
            }

            Subject.Identify(tracks, new IdentificationOverrides(), GivenConfig());

            tracks.Should().OnlyContain(t => t.FileTagInfo.IssueTitle == "A Larger World");
        }

        [Test]
        public void tag_id_shared_by_two_files_should_stay_trusted()
        {
            // A pair is a legitimate duplicate copy (e.g. .cbr + .cbz)
            var tracks = new List<LocalIssue>
            {
                GivenTaggedTrack("cv:338482")[0],
                GivenTaggedTrack("cv:338482")[0]
            };

            Subject.Identify(tracks, new IdentificationOverrides(), GivenConfig());

            tracks.Should().OnlyContain(t => t.FileTagInfo.ForeignIssueId == "cv:338482");
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
