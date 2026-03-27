using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class MonitorNewBookServiceFixture : CoreTest<MonitorNewBookService>
    {
        private List<Issue> _books;

        [SetUp]
        public void Setup()
        {
            _books = Builder<Issue>.CreateListOfSize(4)
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
        }

        [Test]
        public void should_monitor_with_all()
        {
            foreach (var issue in _books)
            {
                Subject.ShouldMonitorNewBook(issue, _books, NewItemMonitorTypes.All).Should().BeTrue();
            }
        }

        [Test]
        public void should_not_monitor_with_none()
        {
            foreach (var issue in _books)
            {
                Subject.ShouldMonitorNewBook(issue, _books, NewItemMonitorTypes.None).Should().BeFalse();
            }
        }

        [Test]
        public void should_only_monitor_new_with_new()
        {
            Subject.ShouldMonitorNewBook(_books[0], _books, NewItemMonitorTypes.New).Should().BeTrue();

            foreach (var issue in _books.Skip(1))
            {
                Subject.ShouldMonitorNewBook(issue, _books, NewItemMonitorTypes.New).Should().BeFalse();
            }
        }
    }
}
