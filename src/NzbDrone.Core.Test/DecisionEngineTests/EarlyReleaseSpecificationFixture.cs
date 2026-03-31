using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.TorrentRss;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class EarlyReleaseSpecificationFixture : TestBase<EarlyReleaseSpecification>
    {
        private Series _series;
        private Issue _issue1;
        private Issue _issue2;
        private RemoteIssue _remoteIssue;
        private IndexerDefinition _indexerDefinition;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew().With(s => s.Id = 1).Build();
            _issue1 = Builder<Issue>.CreateNew().With(s => s.ReleaseDate = DateTime.Today).Build();
            _issue2 = Builder<Issue>.CreateNew().With(s => s.ReleaseDate = DateTime.Today).Build();

            _remoteIssue = new RemoteIssue
            {
                Series = _series,
                Issues = new List<Issue> { _issue1 },
                Release = new TorrentInfo
                {
                    IndexerId = 1,
                    Title = "Series - Issue [FLAC-RlsGrp]",
                    PublishDate = DateTime.Today
                }
            };

            _indexerDefinition = new IndexerDefinition
            {
                Settings = new TorrentRssIndexerSettings { EarlyReleaseLimit = 5 }
            };

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(v => v.Get(1))
                  .Returns(_indexerDefinition);
        }

        private void GivenPublishDateFromToday(int days)
        {
            _remoteIssue.Release.PublishDate = DateTime.Today.AddDays(days);
        }

        [Test]
        public void should_return_true_if_indexer_not_specified()
        {
            _remoteIssue.Release.IndexerId = 0;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_release_contains_multiple_books()
        {
            _remoteIssue.Issues.Add(_issue2);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_indexer_no_longer_exists()
        {
            Mocker.GetMock<IIndexerFactory>()
                  .Setup(v => v.Get(It.IsAny<int>()))
                  .Callback<int>(i => { throw new ModelNotFoundException(typeof(IndexerDefinition), i); });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [TestCase(-2)]
        [TestCase(-5)]
        public void should_return_true_if_publish_date_above_or_equal_to_limit(int days)
        {
            GivenPublishDateFromToday(days);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [TestCase(-10)]
        [TestCase(-20)]
        public void should_return_false_if_publish_date_belove_limit(int days)
        {
            GivenPublishDateFromToday(days);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [TestCase(-10)]
        [TestCase(-100)]
        public void should_return_true_if_limit_null(int days)
        {
            GivenPublishDateFromToday(days);

            _indexerDefinition.Settings = new TorrentRssIndexerSettings { EarlyReleaseLimit = null };

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }
    }
}
