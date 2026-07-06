using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Issues.Relations;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class SeriesRelationServiceFixture : CoreTest<SeriesRelationService>
    {
        private SeriesRelation _relation;

        [SetUp]
        public void Setup()
        {
            _relation = new SeriesRelation
            {
                SeriesId = 1,
                RelatedSeriesId = 2,
                RelationType = SeriesRelationType.Annual
            };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<IEnumerable<int>>()))
                  .Returns((IEnumerable<int> ids) => ids.Select(id => new Series { Id = id }).ToList());

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Setup(s => s.FindBySeriesId(It.IsAny<int>()))
                  .Returns(new List<SeriesRelation>());

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Setup(s => s.Insert(It.IsAny<SeriesRelation>()))
                  .Returns<SeriesRelation>(r => r);
        }

        [Test]
        public void should_add_a_relation()
        {
            var result = Subject.Add(_relation);

            result.Should().BeSameAs(_relation);

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Verify(s => s.Insert(_relation), Times.Once);
        }

        [Test]
        public void should_reject_a_self_link()
        {
            _relation.RelatedSeriesId = _relation.SeriesId;

            Assert.Throws<ArgumentException>(() => Subject.Add(_relation));
        }

        [Test]
        public void should_reject_when_a_series_is_missing()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Series> { new Series { Id = 1 } });

            Assert.Throws<ArgumentException>(() => Subject.Add(_relation));
        }

        [Test]
        public void should_reject_a_duplicate_link_in_either_direction()
        {
            Mocker.GetMock<ISeriesRelationRepository>()
                  .Setup(s => s.FindBySeriesId(1))
                  .Returns(new List<SeriesRelation>
                  {
                      new SeriesRelation { Id = 5, SeriesId = 2, RelatedSeriesId = 1, RelationType = SeriesRelationType.Related }
                  });

            Assert.Throws<ArgumentException>(() => Subject.Add(_relation));
        }

        [Test]
        public void should_allow_linking_different_series_to_the_same_parent()
        {
            Mocker.GetMock<ISeriesRelationRepository>()
                  .Setup(s => s.FindBySeriesId(1))
                  .Returns(new List<SeriesRelation>
                  {
                      new SeriesRelation { Id = 5, SeriesId = 1, RelatedSeriesId = 3, RelationType = SeriesRelationType.Annual }
                  });

            Subject.Add(_relation);

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Verify(s => s.Insert(_relation), Times.Once);
        }

        [Test]
        public void should_delete_relations_when_a_series_is_deleted()
        {
            var relations = new List<SeriesRelation>
            {
                new SeriesRelation { Id = 5, SeriesId = 1, RelatedSeriesId = 2 },
                new SeriesRelation { Id = 6, SeriesId = 3, RelatedSeriesId = 1 }
            };

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Setup(s => s.FindBySeriesId(1))
                  .Returns(relations);

            Subject.HandleAsync(new SeriesDeletedEvent(new Series { Id = 1 }, false, false));

            Mocker.GetMock<ISeriesRelationRepository>()
                  .Verify(s => s.DeleteMany(relations), Times.Once);
        }
    }
}
