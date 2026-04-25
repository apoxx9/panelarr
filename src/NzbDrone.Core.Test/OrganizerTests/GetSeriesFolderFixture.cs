using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests
{
    [TestFixture]

    public class GetSeriesFolderFixture : CoreTest<FileNameBuilder>
    {
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);
        }

        [TestCase("Avenged Sevenfold", "{Series Name}", "Avenged Sevenfold")]
        [TestCase("Avenged Sevenfold", "{Series.Name}", "Avenged.Sevenfold")]
        [TestCase("AC/DC", "{Series Name}", "AC+DC")]
        [TestCase("In the Woods...", "{Series.Name}", "In.the.Woods")]
        [TestCase("3OH!3", "{Series.Name}", "3OH!3")]
        [TestCase("Avenged Sevenfold", ".{Series.Name}.", "Avenged.Sevenfold")]
        public void should_use_seriesFolderFormat_to_build_folder_name(string seriesName, string format, string expected)
        {
            _namingConfig.SeriesFolderFormat = format;

            var series = new Series { Name = seriesName };

            Subject.GetSeriesFolder(series).Should().Be(expected);
        }
    }
}
