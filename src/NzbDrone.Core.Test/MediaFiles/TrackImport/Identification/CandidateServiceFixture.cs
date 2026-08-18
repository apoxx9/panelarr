using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class CandidateServiceFixture : CoreTest<CandidateService>
    {
        private static LocalEdition GivenCollectedEditionFile(string seriesIndex)
        {
            return new LocalEdition
            {
                LocalIssues = new List<LocalIssue>
                {
                    new LocalIssue
                    {
                        Path = @"C:\downloads\ASM Epic Collection Vol. 21 - Web of Life (2021).cbr",
                        FileTagInfo = new ParsedFileTagInfo
                        {
                            Series = new List<string> { "ASM Epic Collection" },
                            SeriesIndex = seriesIndex,
                            IsCollectedEdition = true
                        }
                    }
                }
            };
        }

        private static Series GivenSeries()
        {
            return new Series { SeriesMetadataId = 7 };
        }

        private void GivenSeriesIssues(params Issue[] issues)
        {
            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeriesMetadataId(7))
                  .Returns(new List<Issue>(issues));

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetCandidates(7, It.IsAny<string>()))
                  .Returns(new List<Issue>());
        }

        [Test]
        public void collected_edition_falls_back_to_the_sole_issue_when_volume_number_matches_nothing()
        {
            GivenSeriesIssues(new Issue { Id = 42, IssueNumber = "1" });

            var candidates = Subject.GetDbCandidatesFromTags(
                GivenCollectedEditionFile("21"),
                new IdentificationOverrides { Series = GivenSeries() },
                includeExisting: false);

            var candidate = candidates.Should().ContainSingle().Subject;
            candidate.Issue.Id.Should().Be(42);
            candidate.SoleIssueFallback.Should().BeTrue();
        }

        [Test]
        public void no_sole_issue_fallback_when_series_has_multiple_issues()
        {
            GivenSeriesIssues(
                new Issue { Id = 42, IssueNumber = "1" },
                new Issue { Id = 43, IssueNumber = "2" });

            var candidates = Subject.GetDbCandidatesFromTags(
                GivenCollectedEditionFile("21"),
                new IdentificationOverrides { Series = GivenSeries() },
                includeExisting: false);

            candidates.Should().BeEmpty();
        }

        [Test]
        public void no_sole_issue_fallback_for_a_plain_issue_file()
        {
            GivenSeriesIssues(new Issue { Id = 42, IssueNumber = "1" });

            var edition = GivenCollectedEditionFile("21");
            edition.LocalIssues[0].FileTagInfo.IsCollectedEdition = false;

            var candidates = Subject.GetDbCandidatesFromTags(
                edition,
                new IdentificationOverrides { Series = GivenSeries() },
                includeExisting: false);

            candidates.Should().BeEmpty();
        }

        [Test]
        public void matching_volume_number_still_matches_directly_without_fallback_flag()
        {
            GivenSeriesIssues(
                new Issue { Id = 42, IssueNumber = "1" },
                new Issue { Id = 44, IssueNumber = "3" });

            var candidates = Subject.GetDbCandidatesFromTags(
                GivenCollectedEditionFile("3"),
                new IdentificationOverrides { Series = GivenSeries() },
                includeExisting: false);

            var candidate = candidates.Should().ContainSingle().Subject;
            candidate.Issue.Id.Should().Be(44);
            candidate.SoleIssueFallback.Should().BeFalse();
        }

        [Test]
        public void should_not_throw_on_search_exception()
        {
            Mocker.GetMock<ISearchForNewIssue>()
                .Setup(s => s.SearchForNewIssue(It.IsAny<string>(), It.IsAny<string>(), true))
                .Throws(new Exception("Bad search"));

            var edition = new LocalEdition
            {
                LocalIssues = new List<LocalIssue>
                {
                    new LocalIssue
                    {
                        FileTagInfo = new ParsedFileTagInfo
                        {
                            Series = new List<string> { "Series" },
                            IssueTitle = "Issue"
                        }
                    }
                }
            };

            Subject.GetRemoteCandidates(edition, null).Should().BeEmpty();
        }
    }
}
