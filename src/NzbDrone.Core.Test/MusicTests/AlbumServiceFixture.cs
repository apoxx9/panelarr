using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueRepositoryTests
{
    [TestFixture]
    public class IssueServiceFixture : CoreTest<IssueService>
    {
        private List<Issue> _books;

        [SetUp]
        public void Setup()
        {
            _books = new List<Issue>();
            _books.Add(new Issue
            {
                Title = "ANThology",
                CleanTitle = "anthology",
                SeriesMetadata = new SeriesMetadata
                {
                    Name = "Series"
                }
            });

            _books.Add(new Issue
            {
                Title = "+",
                CleanTitle = "",
                SeriesMetadata = new SeriesMetadata
                {
                    Name = "Series"
                }
            });

            Mocker.GetMock<IBookRepository>()
                .Setup(s => s.GetBooksBySeriesMetadataId(It.IsAny<int>()))
                .Returns(_books);
        }

        private void GivenSimilarBook()
        {
            _books.Add(new Issue
            {
                Title = "ANThology2",
                CleanTitle = "anthology2",
                SeriesMetadata = new SeriesMetadata
                {
                    Name = "Series"
                }
            });
        }

        [TestCase("ANTholog", "ANThology")]
        [TestCase("antholoyg", "ANThology")]
        [TestCase("ANThology CD", "ANThology")]
        [TestCase("ANThology CD xxxx (Remastered) - [Oh please why do they do this?]", "ANThology")]
        [TestCase("+ (Plus) - I feel the need for redundant information in the title field", "+")]
        public void should_find_book_in_db_by_inexact_title(string title, string expected)
        {
            var issue = Subject.FindByTitleInexact(0, title);

            issue.Should().NotBeNull();
            issue.Title.Should().Be(expected);
        }

        [TestCase("ANTholog")]
        [TestCase("antholoyg")]
        [TestCase("ANThology CD")]
        [TestCase("÷")]
        [TestCase("÷ (Divide)")]
        public void should_not_find_book_in_db_by_inexact_title_when_two_similar_matches(string title)
        {
            GivenSimilarBook();
            var issue = Subject.FindByTitleInexact(0, title);

            issue.Should().BeNull();
        }
    }
}
