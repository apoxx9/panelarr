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
    public class TitleTheFixture : CoreTest<FileNameBuilder>
    {
        private Series _series;
        private Issue _issue;
        private ComicFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>
                    .CreateNew()
                    .With(s => s.Name = "Alien Ant Farm")
                    .Build();

            var series = Builder<SeriesGroup>
                .CreateNew()
                .With(x => x.Title = "SeriesGroup Title")
                .Build();

            var seriesLink = Builder<SeriesGroupLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = "1-2")
                .With(s => s.SeriesGroup = series)
                .BuildListOfNew();

            _issue = Builder<Issue>
                    .CreateNew()
                    .With(s => s.Title = "Anthology")
                    .With(s => s.SeriesMetadata = _series.Metadata.Value)
                    .With(s => s.SeriesLinks = seriesLink)
                    .Build();
            _trackFile = new ComicFile { Quality = new QualityModel(Quality.CBR), ReleaseGroup = "PanelarrTest" };

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

        [TestCase("The Mist", "Mist, The")]
        [TestCase("A Place to Call Home", "Place to Call Home, A")]
        [TestCase("An Adventure in Space and Time", "Adventure in Space and Time, An")]
        [TestCase("The Flash (2010)", "Flash, The (2010)")]
        [TestCase("A League Of Their Own (AU)", "League Of Their Own, A (AU)")]
        [TestCase("The Fixer (ZH) (2015)", "Fixer, The (ZH) (2015)")]
        [TestCase("The Sixth Sense 2 (Thai)", "Sixth Sense 2, The (Thai)")]
        [TestCase("The Amazing Race (Latin America)", "Amazing Race, The (Latin America)")]
        [TestCase("The Rat Pack (A&E)", "Rat Pack, The (A&E)")]
        [TestCase("The Climax: I (Almost) Got Away With It (2016)", "Climax - I (Almost) Got Away With It, The (2016)")]
        public void should_get_expected_title_back(string name, string expected)
        {
            _series.Name = name;
            _namingConfig.StandardIssueFormat = "{Series NameThe}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(expected);
        }

        [TestCase("A")]
        [TestCase("Anne")]
        [TestCase("Theodore")]
        [TestCase("3%")]
        public void should_not_change_title(string name)
        {
            _series.Name = name;
            _namingConfig.StandardIssueFormat = "{Series NameThe}";

            Subject.BuildComicFileName(_series, _issue, _trackFile)
                   .Should().Be(name);
        }
    }
}
