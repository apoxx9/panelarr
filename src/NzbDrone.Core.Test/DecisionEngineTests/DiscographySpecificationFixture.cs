using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class DiscographySpecificationFixture : CoreTest<DiscographySpecification>
    {
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            var series = Builder<Series>.CreateNew().With(s => s.Id = 1234).Build();
            _remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = new ParsedIssueInfo
                {
                    Discography = true
                },
                Issues = Builder<Issue>.CreateListOfSize(3)
                                           .All()
                                           .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-8))
                                           .With(s => s.SeriesId = series.Id)
                                           .BuildList(),
                Series = series,
                Release = new ReleaseInfo
                {
                    Title = "Series.Discography.1978.2005.FLAC-RlsGrp"
                }
            };

            Mocker.GetMock<IIssueService>().Setup(s => s.IssuesBetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>(), false))
                                             .Returns(new List<Issue>());
        }

        [Test]
        public void should_return_true_if_is_not_a_discography()
        {
            _remoteIssue.ParsedIssueInfo.Discography = false;
            _remoteIssue.Issues.Last().ReleaseDate = DateTime.UtcNow.AddDays(+2);
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_all_books_have_released()
        {
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_one_book_has_not_released()
        {
            _remoteIssue.Issues.Last().ReleaseDate = DateTime.UtcNow.AddDays(+2);
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_an_book_does_not_have_an_release_date()
        {
            _remoteIssue.Issues.Last().ReleaseDate = null;
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }
    }
}
