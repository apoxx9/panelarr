using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class YearMatchSpecificationFixture : CoreTest<YearMatchSpecification>
    {
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            _remoteIssue = new RemoteIssue
            {
                Release = new ReleaseInfo { Title = "Green Lantern Corps #18 (2026)" },
                ParsedIssueInfo = new ParsedIssueInfo
                {
                    SeriesName = "Green Lantern Corps",
                    SeriesTitleInfo = new SeriesTitleInfo { Title = "Green Lantern Corps" }
                },
                Series = new Series { Metadata = new SeriesMetadata { Year = 2011 } },
                Issues = new List<Issue>()
            };
        }

        private void GivenParsedYear(int year)
        {
            _remoteIssue.ParsedIssueInfo.SeriesTitleInfo.Year = year;
        }

        private void GivenIssueYear(int year)
        {
            _remoteIssue.Issues.Add(new Issue { ReleaseDate = new DateTime(year, 5, 15) });
        }

        [Test]
        public void should_accept_when_release_has_no_year()
        {
            GivenIssueYear(2013);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_release_year_matches_issue_year()
        {
            GivenParsedYear(2013);
            GivenIssueYear(2013);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_release_year_is_one_off_the_issue_year()
        {
            GivenParsedYear(2014);
            GivenIssueYear(2013);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_release_year_matches_series_year_instead_of_issue_year()
        {
            // "Batman #100 (2016)" naming: series started 2016, issue is from 2020
            _remoteIssue.Series.Metadata.Value.Year = 2016;
            GivenParsedYear(2016);
            GivenIssueYear(2020);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_release_year_matches_neither_issue_nor_series_year()
        {
            // The field case: 2026 release of the current series mapped onto
            // the 2011 series' #18 from 2013
            GivenParsedYear(2026);
            GivenIssueYear(2013);

            var decision = Subject.IsSatisfiedBy(_remoteIssue, null);

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Contain("2026");
        }

        [Test]
        public void should_accept_when_issues_have_no_release_date_even_if_series_year_differs()
        {
            // A brand-new issue of a long-running series has no CV date yet
            // and its release year never equals the series start year -
            // undated issues are absence of evidence, not a mismatch
            // (observed live: The Walking Dead Deluxe #141 (2026) vs 2020).
            GivenParsedYear(2026);
            _remoteIssue.Issues.Add(new Issue { ReleaseDate = null });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_no_issue_dates_and_no_series_year_to_compare()
        {
            _remoteIssue.Series.Metadata.Value.Year = null;
            GivenParsedYear(2026);
            _remoteIssue.Issues.Add(new Issue { ReleaseDate = null });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_any_of_multiple_issues_matches_the_year()
        {
            GivenParsedYear(2020);
            GivenIssueYear(2013);
            GivenIssueYear(2020);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }
    }
}
