using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ExtendedQualityParserRegex : CoreTest
    {
        [TestCase("Saga.005.2012.Digital-Empire", 0)]
        [TestCase("East.of.West.005.Garnets.or.Gold.REAL.REAL.PROPER.2014.Digital-Empire", 2)]
        [TestCase("Batman.017.REAL.PROPER.2016.Digital-ORENJI-RP", 1)]
        [TestCase("Saga.009.REAL.PROPER.2013.Digital-KILLERS", 1)]
        [TestCase("East.of.West.014.REAL.PROPER.2015.Digital-KILLERS", 1)]
        [TestCase("Monstress.006.real.proper.2016.digital-2hd", 0)]
        [TestCase("Saga.021.Super.Duper.Real.Proper.2014.Digital-FTP", 0)]
        [TestCase("Saga.022.PROPER.2014.Digital-RiVER-RP", 0)]
        [TestCase("Batman.011.PROPER.REAL.RERIP.2017.Digital-TENEIGHTY", 1)]
        [TestCase("[MGS] - Kuragehime - Episode 02v2 - [D8B6C90D]", 0)]
        [TestCase("[Hatsuyuki] Tokyo Ghoul - 07 [v2][848x480][23D8F455].avi", 0)]
        [TestCase("[DeadFish] Barakamon - 01v3 [720p][AAC]", 0)]
        [TestCase("[DeadFish] Momo Kyun Sword - 01v4 [720p][AAC]", 0)]
        [TestCase("The Real Heroes of Some Place - 001 - Why are we doing this?", 0)]
        public void should_parse_reality_from_title(string title, int reality)
        {
            QualityParser.ParseQuality(title).Revision.Real.Should().Be(reality);
        }

        [TestCase("Saga.005.2012.Digital-Empire", 1)]
        [TestCase("East.of.West.005.Garnets.or.Gold.REAL.REAL.PROPER.2014.Digital-Empire", 2)]
        [TestCase("Batman.017.REAL.PROPER.2016.Digital-ORENJI-RP", 2)]
        [TestCase("Saga.009.REAL.PROPER.2013.Digital-KILLERS", 2)]
        [TestCase("East.of.West.014.REAL.PROPER.2015.Digital-KILLERS", 2)]
        [TestCase("Monstress.006.real.proper.2016.digital-2hd", 2)]
        [TestCase("Saga.021.Super.Duper.Real.Proper.2014.Digital-FTP", 2)]
        [TestCase("Saga.022.PROPER.2014.Digital-RiVER-RP", 2)]
        [TestCase("Batman.011.PROPER.REAL.RERIP.2017.Digital-TENEIGHTY", 2)]
        [TestCase("[MGS] - Kuragehime - Episode 02v2 - [D8B6C90D]", 2)]
        [TestCase("[Hatsuyuki] Tokyo Ghoul - 07 [v2][848x480][23D8F455].avi", 2)]
        [TestCase("[DeadFish] Momo Kyun Sword - 01v4 [720p][AAC]", 4)]
        [TestCase("[Vivid-Asenshi] Akame ga Kill - 04v2 [266EE983]", 2)]
        [TestCase("[Vivid-Asenshi] Akame ga Kill - 03v2 [66A05817]", 2)]
        [TestCase("[Vivid-Asenshi] Akame ga Kill - 02v2 [1F67AB55]", 2)]
        public void should_parse_version_from_title(string title, int version)
        {
            QualityParser.ParseQuality(title).Revision.Version.Should().Be(version);
        }
    }
}
