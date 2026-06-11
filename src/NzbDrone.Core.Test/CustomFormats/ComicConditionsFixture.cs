using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.CustomFormats
{
    [TestFixture]
    public class ComicConditionsFixture : CoreTest<CustomFormatCalculationService>
    {
        private CustomFormat _pageCountFormat;
        private CustomFormat _resolutionFormat;
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _pageCountFormat = new CustomFormat("TPB-sized", new PageCountCondition
            {
                Name = "100+",
                MinPages = 100,
                MaxPages = 99999
            })
            {
                Id = 1
            };

            _resolutionFormat = new CustomFormat("HD", new ComicImageResolutionCondition
            {
                Name = "HD",
                MinScore = 0.8,
                MaxScore = 1.001
            })
            {
                Id = 2
            };

            _series = new Series { Name = "Saga" };

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(s => s.All())
                  .Returns(new List<CustomFormat> { _pageCountFormat, _resolutionFormat });
        }

        private ComicFile GivenComicFile(int imageCount, float imageQualityScore)
        {
            return new ComicFile
            {
                Path = "/comics/Saga/Saga TPB 01.cbz",
                Quality = new QualityModel(Quality.CBZ),
                ImageCount = imageCount,
                ImageQualityScore = imageQualityScore
            };
        }

        [Test]
        public void inspected_file_should_match_comic_conditions()
        {
            var formats = Subject.ParseCustomFormat(GivenComicFile(150, 0.95f), _series);

            formats.Should().Contain(_pageCountFormat);
            formats.Should().Contain(_resolutionFormat);
        }

        [Test]
        public void inspected_file_outside_ranges_should_not_match()
        {
            var formats = Subject.ParseCustomFormat(GivenComicFile(22, 0.4f), _series);

            formats.Should().BeEmpty();
        }

        [Test]
        public void uninspected_file_should_not_match_comic_conditions()
        {
            // 0 means the archive was never inspected — unknown, not "zero pages"
            var formats = Subject.ParseCustomFormat(GivenComicFile(0, 0f), _series);

            formats.Should().BeEmpty();
        }

        [Test]
        public void page_count_condition_should_not_match_null_input()
        {
            var condition = new PageCountCondition { Name = "any", MinPages = 0, MaxPages = 99999 };

            condition.IsSatisfiedBy(new CustomFormatInput()).Should().BeFalse();
        }
    }
}
