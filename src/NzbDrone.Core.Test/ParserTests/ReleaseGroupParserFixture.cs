using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ReleaseGroupParserFixture : CoreTest
    {
        [TestCase("East.of.West-The.Promise-WEB-2018-ENTiTLED", "ENTiTLED")]
        [TestCase("[ www.Torrenting.com ] - East.of.West-The.Promise-WEB-2018-ENTiTLED", "ENTiTLED")]
        [TestCase("East.of.West-The.Promise-WEB-2018-ENTiTLED [eztv]-[rarbg.com]", "ENTiTLED")]
        [TestCase("7s-atlantis-128.cbz", null)]
        [TestCase("East.of.West-The.Promise-WEB-2018-ENTiTLED-Pre", "ENTiTLED")]
        [TestCase("East.of.West-The.Promise-WEB-2018-ENTiTLED-postbot", "ENTiTLED")]
        [TestCase("East.of.West-The.Promise-WEB-2018-ENTiTLED-xpost", "ENTiTLED")]

        //[TestCase("", "")]
        public void should_parse_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [Test]
        [Ignore("Track name parsing needs to be worked on")]
        public void should_not_include_extension_in_release_group()
        {
            const string path = @"C:\Test\Saga.2012.003.internal.digital-archivist.cbz";

            Parser.Parser.ParseFilePath(path).ReleaseGroup.Should().Be("archivist");
        }

        [TestCase("East.of.West-The.Promise-WEB-2018-SKGTV English", "SKGTV")]
        [TestCase("East.of.West-The.Promise-WEB-2018-SKGTV_English", "SKGTV")]
        [TestCase("East.of.West-The.Promise-WEB-2018-SKGTV.English", "SKGTV")]

        //[TestCase("", "")]
        public void should_not_include_language_in_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("East.of.West-The.Promise-WEB-2018-EVL-RP", "EVL")]
        [TestCase("East.of.West-The.Promise-WEB-2018-EVL-RP-RP", "EVL")]
        [TestCase("East.of.West-The.Promise-WEB-2018-EVL-Obfuscated", "EVL")]
        [TestCase("East.of.West-The.Promise-WEB-2018-xHD-NZBgeek", "xHD")]
        [TestCase("East.of.West-The.Promise-WEB-2018-DIMENSION-NZBgeek", "DIMENSION")]
        [TestCase("East.of.West-The.Promise-WEB-2018-xHD-1", "xHD")]
        [TestCase("East.of.West-The.Promise-WEB-2018-DIMENSION-1", "DIMENSION")]
        [TestCase("East.of.West-The.Promise-WEB-2018-EVL-Scrambled", "EVL")]
        public void should_not_include_repost_in_release_group(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }

        [TestCase("[FFF] Invaders of the Rokujouma!! - 011 - Someday, With Them", "FFF")]
        [TestCase("[HorribleSubs] Invaders of the Rokujouma!! - 012 - Invasion Going Well!!", "HorribleSubs")]
        [TestCase("[Anime-Koi] Barakamon - 006 - Guys From Tokyo", "Anime-Koi")]
        [TestCase("[Anime-Koi] Barakamon - 007 - A High-Grade Fish", "Anime-Koi")]
        [TestCase("[Anime-Koi] Kami-sama Hajimemashita 2 - 01 [h264-720p][28D54E2C]", "Anime-Koi")]

        //[TestCase("Tokyo.Ghoul.02x01.013.HDTV-720p-Anime-Koi", "Anime-Koi")]
        //[TestCase("", "")]
        public void should_parse_anime_release_groups(string title, string expected)
        {
            Parser.Parser.ParseReleaseGroup(title).Should().Be(expected);
        }
    }
}
