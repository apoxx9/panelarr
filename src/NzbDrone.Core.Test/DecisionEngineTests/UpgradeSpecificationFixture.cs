using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class UpgradeSpecificationFixture : CoreTest<UpgradableSpecification>
    {
        public static object[] IsUpgradeTestCases =
        {
            new object[] { Quality.CBZ, 1, Quality.CBZ, 2, Quality.CBZ, true },
            new object[] { Quality.CBR, 1, Quality.CBR, 2, Quality.CBR, true },
            new object[] { Quality.CBR, 1, Quality.CBR, 1, Quality.CBR, false },
            new object[] { Quality.CBR, 1, Quality.CBZ, 2, Quality.CBR, true },
            new object[] { Quality.CBZ, 1, Quality.CBR, 2, Quality.CBZ, false },
            new object[] { Quality.CBR, 1, Quality.CBR, 1, Quality.CBR, false }
        };

        private void GivenAutoDownloadPropers(ProperDownloadTypes type)
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.DownloadPropersAndRepacks)
                  .Returns(type);
        }

        [Test]
        [TestCaseSource(nameof(IsUpgradeTestCases))]
        public void IsUpgradeTest(Quality current, int currentVersion, Quality newQuality, int newVersion, Quality cutoff, bool expected)
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.PreferAndUpgrade);

            var profile = new QualityProfile
            {
                UpgradeAllowed = true,
                Items = Qualities.QualityFixture.GetDefaultQualities()
            };

            Subject.IsUpgradable(
                        profile,
                        new QualityModel(current, new Revision(version: currentVersion)),
                        new List<CustomFormat>(),
                        new QualityModel(newQuality, new Revision(version: newVersion)),
                        new List<CustomFormat>())
                   .Should().Be(expected);
        }

        [Test]
        public void should_return_true_if_proper_and_download_propers_is_do_not_download()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.DoNotUpgrade);

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
            };

            Subject.IsUpgradable(
                        profile,
                        new QualityModel(Quality.CBR, new Revision(version: 1)),
                        new List<CustomFormat>(),
                        new QualityModel(Quality.CBR, new Revision(version: 2)),
                        new List<CustomFormat>())
                    .Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_proper_and_autoDownloadPropers_is_do_not_prefer()
        {
            GivenAutoDownloadPropers(ProperDownloadTypes.DoNotPrefer);

            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(),
            };

            Subject.IsUpgradable(
                        profile,
                        new QualityModel(Quality.CBR, new Revision(version: 1)),
                        new List<CustomFormat>(),
                        new QualityModel(Quality.CBR, new Revision(version: 2)),
                        new List<CustomFormat>())
                    .Should().BeFalse();
        }
    }
}
