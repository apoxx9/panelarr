using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteIssue _remoteIssue;

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

            _remoteIssue = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
            };
        }

        [Test]
        [TestCaseSource(nameof(AllowedTestCases))]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteIssue.ParsedIssueInfo.Quality.Quality = qualityType;
            _remoteIssue.Series.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.CBR, Quality.CBR, Quality.CBR);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource(nameof(DeniedTestCases))]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteIssue.ParsedIssueInfo.Quality.Quality = qualityType;
            _remoteIssue.Series.QualityProfile.Value.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.CBR, Quality.CBR, Quality.CBR);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }
    }
}
