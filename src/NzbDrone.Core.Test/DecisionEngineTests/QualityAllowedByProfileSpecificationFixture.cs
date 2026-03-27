using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteBook _remoteBook;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.CBR },
            new object[] { Quality.CBR },
            new object[] { Quality.CBR }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.CBZ_HD },
            new object[] { Quality.Unknown }
        };

        [SetUp]
        public void Setup()
        {
            var fakeSeries = Builder<Series>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.CBR.Id })
                         .Build();

            _remoteBook = new RemoteBook
            {
                Series = fakeSeries,
                ParsedBookInfo = new ParsedBookInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
            };
        }

        [Test]
        [TestCaseSource(nameof(AllowedTestCases))]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = qualityType;
            _remoteBook.Series.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.CBR, Quality.CBR, Quality.CBR);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource(nameof(DeniedTestCases))]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteBook.ParsedBookInfo.Quality.Quality = qualityType;
            _remoteBook.Series.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.CBR, Quality.CBR, Quality.CBR);

            Subject.IsSatisfiedBy(_remoteBook, null).Accepted.Should().BeFalse();
        }
    }
}
