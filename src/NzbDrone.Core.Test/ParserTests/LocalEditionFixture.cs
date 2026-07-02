using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class LocalEditionFixture : CoreTest
    {
        private LocalEdition GivenMatchedEdition(Issue issue)
        {
            return new LocalEdition(new List<LocalIssue>
            {
                new LocalIssue { Path = @"C:\comics\Saga (2012)\Saga v5 (2015).cbz".AsOsAgnostic() }
            })
            {
                Issue = issue,
                ExistingTracks = new List<LocalIssue>()
            };
        }

        [Test]
        public void populate_match_should_survive_issue_without_series_links()
        {
            // Provider search results are mapper-built issues: SeriesMetadata is
            // set, SeriesLinks never is (see IssueInfoProxy.SearchForNewIssue)
            var issue = new Issue
            {
                ForeignIssueId = "cv:506120",
                Title = "HC",
                SeriesMetadata = new SeriesMetadata { ForeignSeriesId = "cv:86146", Name = "Saga" }
            };

            var edition = GivenMatchedEdition(issue);

            edition.PopulateMatch(false);

            edition.Issue.Should().NotBeNull();
            edition.Issue.ForeignIssueId.Should().Be("cv:506120");
            edition.LocalIssues[0].Issue.Should().Be(edition.Issue);
        }

        [Test]
        public void populate_match_should_clone_loaded_series_links()
        {
            var issue = new Issue
            {
                ForeignIssueId = "cv:1",
                SeriesMetadata = new SeriesMetadata { ForeignSeriesId = "cv:2" },
                SeriesLinks = new List<SeriesGroupLink>
                {
                    new SeriesGroupLink
                    {
                        SeriesGroup = new SeriesGroup { ForeignSeriesGroupId = "cv:g1", Title = "Group" },
                        IsPrimary = true
                    }
                }
            };

            var edition = GivenMatchedEdition(issue);

            edition.PopulateMatch(false);

            edition.Issue.SeriesLinks.Value.Should().HaveCount(1);
            edition.Issue.SeriesLinks.Value[0].SeriesGroup.Value.Title.Should().Be("Group");
        }
    }
}
