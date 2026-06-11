using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class HashedReleaseFixture : CoreTest
    {
        public static object[] HashedReleaseParserCases =
        {
            new object[]
            {
                @"C:\Test\Some.Hashed.Release.(2012).(Digital)-Mercury\0e895c37245186812cb08aab1529cf8ee389dd05.cbr".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.Scan,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test-[256]\0e895c37245186812cb08aab1529cf8ee389dd05\Some.Hashed.Release.003.2012.Digital.WEB-Mercury.cbr".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.Scan,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test\Fake.Dir.003-Test\yrucreM-BEW.latigiD.2102.300.esaeleR.dehsaH.emoS.cbr".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.Scan,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test\Fake.Dir.003-Test\yrucreM-BEW latigiD 2102 300 esaeleR dehsaH emoS.cbr".AsOsAgnostic(),
                "Some Hashed Release",
                Quality.Scan,
                "Mercury"
            },
            new object[]
            {
                @"C:\Test\East.of.West.010.2013.Digital-Panelarr\AHFMZXGHEWD660.cbr".AsOsAgnostic(),
                "East of West",
                Quality.Scan,
                "Panelarr"
            },
            new object[]
            {
                @"C:\Test\Saga.012.2013.Digital-Panelarr\Backup_72023S02-12.cbr".AsOsAgnostic(),
                "Saga",
                Quality.Scan,
                null
            },
            new object[]
            {
                @"C:\Test\Monstress 008 Chupacabra 2016 Digital WEB-ECI\123.cbr".AsOsAgnostic(),
                "Monstress",
                Quality.Scan,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Monstress 008 Chupacabra 2016 Digital WEB-ECI\abc.cbr".AsOsAgnostic(),
                "Monstress",
                Quality.Scan,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Monstress 008 Chupacabra 2016 Digital WEB-ECI\b00bs.cbr".AsOsAgnostic(),
                "Monstress",
                Quality.Scan,
                "ECI"
            },
            new object[]
            {
                @"C:\Test\Paper.Girls.023.2016.Digital-NZBgeek/cgajsofuejsa501.cbr".AsOsAgnostic(),
                "Paper Girls",
                Quality.Scan,
                "NZBgeek"
            }
        };

        [Test]
        [TestCaseSource(nameof(HashedReleaseParserCases))]
        [Ignore("Hashed code is not currently called with track parsing")]
        public void should_properly_parse_hashed_releases(string path, string title, Quality quality, string releaseGroup)
        {
            var result = Parser.Parser.ParseFilePath(path);

            //result.SeriesTitle.Should().Be(title);
            result.Quality.Quality.Should().Be(quality);
        }
    }
}
