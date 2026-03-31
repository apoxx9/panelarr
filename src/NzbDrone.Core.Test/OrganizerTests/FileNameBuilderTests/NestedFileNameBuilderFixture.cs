using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class NestedFileNameBuilderFixture : CoreTest<FileNameBuilder>
    {
        private Series _artist;
        private Issue _album; private ComicFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Series>
                    .CreateNew()
                    .With(s => s.Name = "SeriesName")
                    .With(s => s.Metadata = new SeriesMetadata
                    {
                        Disambiguation = "US Series",
                        Name = "SeriesName"
                    })
                    .Build();

            _album = Builder<Issue>
                .CreateNew()
                .With(s => s.Series = _artist)
                .With(s => s.SeriesMetadata = _artist.Metadata.Value)
                .With(s => s.Title = "A Novel")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .With(s => s.SeriesLinks = new List<SeriesGroupLink>())
                .Build();
            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameComics = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _trackFile = Builder<ComicFile>.CreateNew()
                .With(e => e.Quality = new QualityModel(Quality.CBR))
                .With(e => e.ReleaseGroup = "PanelarrTest")
                .Build();

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        private void WithSeries()
        {
            _album.SeriesLinks = new List<SeriesGroupLink>
            {
                new SeriesGroupLink
                {
                    SeriesGroup = new SeriesGroup
                    {
                        Title = "A SeriesGroup",
                    },
                    Position = "2-3",
                    SeriesPosition = 1
                }
            };
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_forward_slash()
        {
            WithSeries();

            _namingConfig.StandardIssueFormat = "{Issue SeriesGroup}/{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            var name = Subject.BuildComicFileName(_artist, _album, _trackFile)
                .Should().Be("A SeriesGroup\\A SeriesGroup #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_forward_slash()
        {
            _namingConfig.StandardIssueFormat = "{Issue SeriesGroup}/{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildComicFileName(_artist, _album, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_back_slash()
        {
            WithSeries();

            _namingConfig.StandardIssueFormat = "{Issue SeriesGroup}\\{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildComicFileName(_artist, _album, _trackFile)
                   .Should().Be("A SeriesGroup\\A SeriesGroup #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_back_slash()
        {
            _namingConfig.StandardIssueFormat = "{Issue SeriesGroup}\\{Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildComicFileName(_artist, _album, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }
    }
}
