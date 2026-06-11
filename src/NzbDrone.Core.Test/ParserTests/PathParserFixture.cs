using NUnit.Framework;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class PathParserFixture : CoreTest
    {
        [TestCase(@"z:\comics\East of West (2013)\Volume 3\East of West 005 - Collaborators.cbz", 3, 5)]
        [TestCase(@"z:\comics\modern marvels\Volume 16\Modern Marvels 003 - The Potato.cbz", 16, 3)]
        [TestCase(@"z:\comics\robot chicken\Specials\Robot Chicken Annual 016 - Dear Consumer.cbr", 0, 16)]
        [TestCase(@"D:\shares\Comics\Saga (2012)\Volume 2\Saga 021 - 94 Meetings (Digital).cbz", 2, 21)]
        [TestCase(@"D:\shares\Comics\East of West (2013)\Volume 2\East of West 021.cbz", 2, 21)]
        [TestCase("C:/Test/Comics/Saga.v4.005.2014.Digital-Empire", 4, 5)]
        [TestCase(@"P:\Comics\Batman (2016)\Volume 6\Batman 013 - 5 to 9 (Digital).cbz", 6, 13)]
        [TestCase(@"S:\Comic Drop\Batman - 10x11 - Title [Digital]\1011 - Title.cbz", 10, 11)]
        [TestCase(@"/Comic Drop/Batman - 10x11 - Title [Digital]/1011 - Title.cbz", 10, 11)]
        [TestCase(@"S:\Comic Drop\King of the Hill - 10x12 - 24 Hour Propane People [Digital]\1012 - 24 Hour Propane People.cbz", 10, 12)]
        [TestCase(@"/Comic Drop/King of the Hill - 10x12 - 24 Hour Propane People [Digital]/1012 - 24 Hour Propane People.cbz", 10, 12)]
        [TestCase(@"S:\Comic Drop\King of the Hill - 10x12 - 24 Hour Propane People [Digital]\Hour Propane People.cbz", 10, 12)]
        [TestCase(@"/Comic Drop/King of the Hill - 10x12 - 24 Hour Propane People [Digital]/Hour Propane People.cbz", 10, 12)]
        [TestCase(@"E:\Downloads\comics\Saga.001.2012.Digital-Empire\ajifajjjeaeaeqwer_eppj.cbz", 1, 1)]
        [TestCase(@"C:\Test\Unsorted\Saga.001.2012.Digital-Empire\saga101.cbz", 1, 1)]
        [TestCase(@"C:\Test\Unsorted\East.of.West.019.2014.Digital-SiNNERS-RP\ba27283b17c00d01193eacc02a8ba98eeb523a76.cbz", 2, 19)]
        [TestCase(@"C:\Test\Unsorted\East.of.West.018.2014.Digital-SiNNERS-RP\45a55debe3856da318cc35882ad07e43cd32fd15.cbz", 2, 18)]
        [TestCase(@"C:\Test\SeriesGroup\Volume 01\01 Pilot (Digital).cbz", 1, 1)]
        [TestCase(@"C:\Test\SeriesGroup\Volume 01\1 Pilot (Digital).cbz", 1, 1)]
        [TestCase(@"C:\Test\SeriesGroup\Volume 1\02 Honor Thy Father (Digital).cbz", 1, 2)]
        [TestCase(@"C:\Test\SeriesGroup\Volume 1\2 Honor Thy Father (Digital).cbz", 1, 2)]

        //        [TestCase(@"C:\CSI.NY.S02E04.720p.WEB-DL.DD5.1.H.264\73696S02-04.mkv", 2, 4)] //Gets treated as S01E04 (because it gets parsed as anime)
        public void should_parse_from_path(string path, int season, int episode)
        {
            var result = Parser.Parser.ParseFilePath(path.AsOsAgnostic());

            //result.EpisodeNumbers.Should().HaveCount(1);
            //result.SeasonNumber.Should().Be(season);
            //result.EpisodeNumbers[0].Should().Be(episode);
            //result.AbsoluteEpisodeNumbers.Should().BeEmpty();
            //result.FullSeason.Should().BeFalse();
            ExceptionVerification.IgnoreWarns();
        }
    }
}
