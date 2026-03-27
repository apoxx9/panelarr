using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentValidation;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class AddBookFixture : CoreTest<AddBookService>
    {
        private Series _fakeSeries;
        private Issue _fakeBook;

        [SetUp]
        public void Setup()
        {
            _fakeSeries = Builder<Series>
                .CreateNew()
                .With(s => s.Path = null)
                .With(s => s.Metadata = Builder<SeriesMetadata>.CreateNew().Build())
                .Build();
        }

        private void GivenValidBook(string bookId, string editionId)
        {
            _fakeBook = Builder<Issue>
                .CreateNew()
                .With(x => x.ForeignIssueId = bookId)
                .Build();

            Mocker.GetMock<IProvideBookInfo>()
                .Setup(s => s.GetBookInfo(bookId))
                .Returns(Tuple.Create(_fakeSeries.Metadata.Value.ForeignSeriesId,
                                      _fakeBook,
                                      new List<SeriesMetadata> { _fakeSeries.Metadata.Value }));

            Mocker.GetMock<IAddSeriesService>()
                .Setup(s => s.AddSeries(It.IsAny<Series>(), It.IsAny<bool>()))
                .Returns(_fakeSeries);
        }

        private void GivenValidPath()
        {
            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.GetSeriesFolder(It.IsAny<Series>(), null))
                  .Returns<Series, NamingConfig>((c, n) => c.Name);
        }

        private Issue IssueToAdd(string editionId, string bookId, string authorId)
        {
            return new Issue
            {
                ForeignIssueId = bookId,
                SeriesMetadata = new SeriesMetadata
                {
                    ForeignSeriesId = authorId
                }
            };
        }

        [Test]
        public void should_be_able_to_add_a_book_without_passing_in_name()
        {
            var newBook = IssueToAdd("edition", "issue", "author");

            GivenValidBook("issue", "edition");
            GivenValidPath();

            var issue = Subject.AddBook(newBook);

            issue.Title.Should().Be(_fakeBook.Title);
        }

        [Test]
        public void should_throw_if_book_cannot_be_found()
        {
            var newBook = IssueToAdd("edition", "issue", "author");

            Mocker.GetMock<IProvideBookInfo>()
                  .Setup(s => s.GetBookInfo("issue"))
                  .Throws(new IssueNotFoundException("edition"));

            Assert.Throws<ValidationException>(() => Subject.AddBook(newBook));

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
