using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.IssueMonitoredServiceTests
{
    [TestFixture]
    public class SetBookMontitoredFixture : CoreTest<IssueMonitoredService>
    {
        private Series _author;
        private List<Issue> _books;

        [SetUp]
        public void Setup()
        {
            const int issues = 4;

            _author = Builder<Series>.CreateNew()
                                     .Build();

            _books = Builder<Issue>.CreateListOfSize(issues)
                                        .All()
                                        .With(e => e.Monitored = true)
                                        .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(-7))

                                        //Future
                                        .TheFirst(1)
                                        .With(e => e.ReleaseDate = DateTime.UtcNow.AddDays(7))

                                        //Future/TBA
                                        .TheNext(1)
                                        .With(e => e.ReleaseDate = null)
                                        .Build()
                                        .ToList();

            Mocker.GetMock<IBookService>()
                  .Setup(s => s.GetBooksBySeries(It.IsAny<int>()))
                  .Returns(_books);

            Mocker.GetMock<IBookService>()
                .Setup(s => s.GetSeriesBooksWithFiles(It.IsAny<Series>()))
                .Returns(new List<Issue>());
        }

        [Test]
        public void should_be_able_to_monitor_author_without_changing_books()
        {
            Subject.SetBookMonitoredStatus(_author, null);

            Mocker.GetMock<ISeriesService>()
                  .Verify(v => v.UpdateSeries(It.IsAny<Series>()), Times.Once());

            Mocker.GetMock<IBookService>()
                  .Verify(v => v.UpdateMany(It.IsAny<List<Issue>>()), Times.Never());
        }

        [Test]
        public void should_be_able_to_monitor_books_when_passed_in_author()
        {
            var booksToMonitor = new List<string> { _books.First().ForeignIssueId };

            Subject.SetBookMonitoredStatus(_author, new MonitoringOptions { Monitored = true, IssuesToMonitor = booksToMonitor });

            Mocker.GetMock<ISeriesService>()
                .Verify(v => v.UpdateSeries(It.IsAny<Series>()), Times.Once());

            VerifyMonitored(e => e.ForeignIssueId == _books.First().ForeignIssueId);
            VerifyNotMonitored(e => e.ForeignIssueId != _books.First().ForeignIssueId);
        }

        [Test]
        public void should_be_able_to_monitor_all_books()
        {
            Subject.SetBookMonitoredStatus(_author, new MonitoringOptions { Monitor = MonitorTypes.All });

            Mocker.GetMock<IBookService>()
                  .Verify(v => v.UpdateBook(It.Is<Issue>(l => l.Monitored)), Times.Exactly(_books.Count));
        }

        [Test]
        public void should_be_able_to_monitor_new_books_only()
        {
            var monitoringOptions = new MonitoringOptions
            {
                Monitor = MonitorTypes.Future
            };

            Subject.SetBookMonitoredStatus(_author, monitoringOptions);

            VerifyMonitored(e => e.ReleaseDate.HasValue && e.ReleaseDate.Value.After(DateTime.UtcNow));
            VerifyMonitored(e => !e.ReleaseDate.HasValue);
            VerifyNotMonitored(e => e.ReleaseDate.HasValue && e.ReleaseDate.Value.Before(DateTime.UtcNow));
        }

        private void VerifyMonitored(Func<Issue, bool> predicate)
        {
            Mocker.GetMock<IBookService>()
                .Verify(v => v.UpdateBook(It.Is<Issue>(b => b.Monitored)), Times.AtLeast(_books.Where(predicate).Count()));
        }

        private void VerifyNotMonitored(Func<Issue, bool> predicate)
        {
            Mocker.GetMock<IBookService>()
                .Verify(v => v.UpdateBook(It.Is<Issue>(b => !b.Monitored)), Times.AtLeast(_books.Where(predicate).Count()));
        }
    }
}
