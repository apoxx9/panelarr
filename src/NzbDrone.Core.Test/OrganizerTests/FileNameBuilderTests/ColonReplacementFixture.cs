using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class ColonReplacementFixture : CoreTest<FileNameBuilder>
    {
        private Series _series;
        private Issue _issue;
        private ComicFile _comicFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                .CreateNew()
                .With(s => s.Name = "Christopher Hopper")
                .Build();

            var series = Builder<SeriesGroup>
                .CreateNew()
                .With(x => x.Title = "SeriesGroup: Ruins of the Earth")
                .Build();

            var seriesLink = Builder<SeriesGroupLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = "1-2")
                .With(s => s.SeriesGroup = series)
                .BuildListOfNew();

            _issue = Builder<Issue>
                .CreateNew()
                .With(s => s.Title = "Fake: Phantom Deadfall")
                .With(s => s.SeriesMetadata = _series.Metadata.Value)
                .With(s => s.ReleaseDate = new DateTime(2021, 2, 14))
                .With(s => s.SeriesLinks = seriesLink)
                .Build();
            _comicFile = new ComicFile { Quality = new QualityModel(Quality.EPUB), ReleaseGroup = "PanelarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameComics = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_replace_colon_followed_by_space_with_space_dash_space_by_default()
        {
            _namingConfig.StandardIssueFormat = "{Series Name} - {Issue SeriesTitle - }{Issue Title} {(Release Year)}";

            Subject.BuildComicFileName(_series, _issue, _comicFile)
                   .Should().Be("Christopher Hopper - SeriesGroup - Ruins of the Earth #1-2 - Fake - Phantom Deadfall (2021)");
        }

        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Smart, "Christopher Hopper - SeriesGroup - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Dash, "Christopher Hopper - SeriesGroup- Ruins of the Earth - Fake- Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Delete, "Christopher Hopper - SeriesGroup Ruins of the Earth - Fake Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDash, "Christopher Hopper - SeriesGroup - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDashSpace, "Christopher Hopper - SeriesGroup - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        public void should_replace_colon_followed_by_space_with_expected_result(string bookTitle, ColonReplacementFormat replacementFormat, string expected)
        {
            _issue.Title = bookTitle;
            _namingConfig.StandardIssueFormat = "{Series Name} - {Issue SeriesGroup - }{Issue Title} {(Release Year)}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildComicFileName(_series, _issue, _comicFile)
                .Should().Be(expected);
        }

        [TestCase("Series:Name", ColonReplacementFormat.Smart, "Series-Name")]
        [TestCase("Series:Name", ColonReplacementFormat.Dash, "Series-Name")]
        [TestCase("Series:Name", ColonReplacementFormat.Delete, "SeriesName")]
        [TestCase("Series:Name", ColonReplacementFormat.SpaceDash, "Series -Name")]
        [TestCase("Series:Name", ColonReplacementFormat.SpaceDashSpace, "Series - Name")]
        public void should_replace_colon_with_expected_result(string seriesName, ColonReplacementFormat replacementFormat, string expected)
        {
            _series.Name = seriesName;
            _namingConfig.StandardIssueFormat = "{Series Name}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildComicFileName(_series, _issue, _comicFile)
                .Should().Be(expected);
        }
    }
}
