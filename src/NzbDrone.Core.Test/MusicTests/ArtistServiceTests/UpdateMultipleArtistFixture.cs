using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests.SeriesServiceTests
{
    [TestFixture]
    public class UpdateMultipleSeriesFixture : CoreTest<SeriesService>
    {
        private List<Series> _authors;

        [SetUp]
        public void Setup()
        {
            _authors = Builder<Series>.CreateListOfSize(5)
                .All()
                .With(s => s.QualityProfileId = 1)
                .With(s => s.Monitored)
                .With(s => s.Path = @"C:\Test\name".AsOsAgnostic())
                .With(s => s.RootFolderPath = "")
                .Build().ToList();
        }

        [Test]
        public void should_call_repo_updateMany()
        {
            Subject.UpdateSeriess(_authors, false);

            Mocker.GetMock<ISeriesRepository>().Verify(v => v.UpdateMany(_authors), Times.Once());
        }

        [Test]
        public void should_update_path_when_rootFolderPath_is_supplied()
        {
            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetSeriesFolder(It.IsAny<Series>(), null))
                .Returns<Series, NamingConfig>((c, n) => c.Name);

            var newRoot = @"C:\Test\Music2".AsOsAgnostic();
            _authors.ForEach(s => s.RootFolderPath = newRoot);

            Mocker.GetMock<IBuildSeriesPaths>()
                .Setup(s => s.BuildPath(It.IsAny<Series>(), false))
                .Returns<Series, bool>((s, u) => Path.Combine(s.RootFolderPath, s.Name));

            Subject.UpdateSeriess(_authors, false).ForEach(s => s.Path.Should().StartWith(newRoot));
        }

        [Test]
        public void should_not_update_path_when_rootFolderPath_is_empty()
        {
            Subject.UpdateSeriess(_authors, false).ForEach(s =>
            {
                var expectedPath = _authors.Single(ser => ser.Id == s.Id).Path;
                s.Path.Should().Be(expectedPath);
            });
        }

        [Test]
        public void should_be_able_to_update_many_author()
        {
            var author = Builder<Series>.CreateListOfSize(50)
                                        .All()
                                        .With(s => s.Path = (@"C:\Test\Music\" + s.Path).AsOsAgnostic())
                                        .Build()
                                        .ToList();

            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetSeriesFolder(It.IsAny<Series>(), null))
                .Returns<Series, NamingConfig>((c, n) => c.Name);

            var newRoot = @"C:\Test\Music2".AsOsAgnostic();
            author.ForEach(s => s.RootFolderPath = newRoot);

            Subject.UpdateSeriess(author, false);
        }
    }
}
