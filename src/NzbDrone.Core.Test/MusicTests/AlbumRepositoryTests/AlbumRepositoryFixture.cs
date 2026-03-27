using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueRepositoryTests
{
    [TestFixture]
    public class IssueRepositoryFixture : DbTest<IssueService, Issue>
    {
        private Series _author;
        private Issue _book;
        private Issue _bookSpecial;
        private List<Issue> _books;
        private IssueRepository _bookRepo;

        [SetUp]
        public void Setup()
        {
            AssertionOptions.AssertEquivalencyUsing(options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.ToUniversalTime())).WhenTypeIs<DateTime>();
                options.Using<DateTime?>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.Value.ToUniversalTime())).WhenTypeIs<DateTime?>();
                return options;
            });

            _author = new Series
            {
                Name = "Alien Ant Farm",
                Monitored = true,
                ForeignSeriesId = "this is a fake id",
                Id = 1,
                SeriesMetadataId = 1
            };

            _bookRepo = Mocker.Resolve<IssueRepository>();

            _book = new Issue
            {
                Title = "ANThology",
                ForeignIssueId = "1",
                TitleSlug = "1-ANThology",
                CleanTitle = "anthology",
                Series = _author,
                SeriesMetadataId = _author.SeriesMetadataId,
            };

            _bookRepo.Insert(_book);
            _bookRepo.Update(_book);

            _bookSpecial = new Issue
            {
                Title = "+",
                ForeignIssueId = "2",
                TitleSlug = "2-_",
                CleanTitle = "",
                Series = _author,
                SeriesMetadataId = _author.SeriesMetadataId
            };

            _bookRepo.Insert(_bookSpecial);
        }

        [TestCase("ANThology")]
        [TestCase("anthology")]
        [TestCase("anthology!")]
        public void should_find_book_in_db_by_title(string title)
        {
            var issue = _bookRepo.FindByTitle(_author.SeriesMetadataId, title);

            issue.Should().NotBeNull();
            issue.Title.Should().Be(_book.Title);
        }

        [Test]
        public void should_find_book_in_db_by_title_all_special_characters()
        {
            var issue = _bookRepo.FindByTitle(_author.SeriesMetadataId, "+");

            issue.Should().NotBeNull();
            issue.Title.Should().Be(_bookSpecial.Title);
        }

        [TestCase("ANTholog")]
        [TestCase("nthology")]
        [TestCase("antholoyg")]
        [TestCase("÷")]
        public void should_not_find_book_in_db_by_incorrect_title(string title)
        {
            var issue = _bookRepo.FindByTitle(_author.SeriesMetadataId, title);

            issue.Should().BeNull();
        }

        [Test]
        public void should_not_find_book_when_two_books_have_same_name()
        {
            var issues = Builder<Issue>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Series = _author)
                .With(x => x.SeriesMetadataId = _author.SeriesMetadataId)
                .With(x => x.Title = "Weezer")
                .With(x => x.CleanTitle = "weezer")
                .Build();

            _bookRepo.InsertMany(issues);

            var issue = _bookRepo.FindByTitle(_author.SeriesMetadataId, "Weezer");

            _bookRepo.All().Should().HaveCount(4);
            issue.Should().BeNull();
        }

        private void GivenMultipleBooks()
        {
            _books = Builder<Issue>.CreateListOfSize(4)
                .All()
                .With(x => x.Id = 0)
                .With(x => x.Series = _author)
                .With(x => x.SeriesMetadataId = _author.SeriesMetadataId)
                .TheFirst(1)

                // next
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(1))
                .TheNext(1)

                // another future one
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(2))
                .TheNext(1)

                // most recent
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(-1))
                .TheNext(1)

                // an older one
                .With(x => x.ReleaseDate = DateTime.UtcNow.AddDays(-2))
                .BuildList();

            _bookRepo.InsertMany(_books);
        }

        [Test]
        public void get_next_books_should_return_next_book()
        {
            GivenMultipleBooks();

            var result = _bookRepo.GetNextBooks(new[] { _author.SeriesMetadataId });
            result.Should().BeEquivalentTo(_books.Take(1), IssueComparerOptions);
        }

        [Test]
        public void get_last_books_should_return_next_book()
        {
            GivenMultipleBooks();

            var result = _bookRepo.GetLastBooks(new[] { _author.SeriesMetadataId });
            result.Should().BeEquivalentTo(_books.Skip(2).Take(1), IssueComparerOptions);
        }

        private EquivalencyAssertionOptions<Issue> IssueComparerOptions(EquivalencyAssertionOptions<Issue> opts) => opts.ComparingByMembers<Issue>()
                .Excluding(ctx => ctx.SelectedMemberInfo.MemberType.IsGenericType && ctx.SelectedMemberInfo.MemberType.GetGenericTypeDefinition() == typeof(LazyLoaded<>))
                .Excluding(x => x.SeriesId)
                .Excluding(x => x.ForeignIssueId);
    }
}
