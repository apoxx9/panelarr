using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Profiles.Delay
{
    [TestFixture]
    public class DelayProfileFixture : TestBase
    {
        private static DelayProfile Profile()
        {
            return new DelayProfile
            {
                EnableUsenet = true,
                EnableTorrent = true,
                EnableDirectDownload = true,
                UsenetDelay = 5,
                TorrentDelay = 10,
                DirectDownloadDelay = 30
            };
        }

        [Test]
        public void direct_download_has_its_own_delay()
        {
            // Previously anything that was not torrent fell through to the
            // usenet delay - a direct download could never be delayed on its
            // own terms.
            var profile = Profile();

            profile.GetProtocolDelay(DownloadProtocol.Torrent).Should().Be(10);
            profile.GetProtocolDelay(DownloadProtocol.DirectDownload).Should().Be(30);
            profile.GetProtocolDelay(DownloadProtocol.Usenet).Should().Be(5);
        }

        [Test]
        public void direct_download_can_be_disabled_independently()
        {
            var profile = Profile();
            profile.EnableDirectDownload = false;

            profile.IsProtocolEnabled(DownloadProtocol.DirectDownload).Should().BeFalse();
            profile.IsProtocolEnabled(DownloadProtocol.Torrent).Should().BeTrue();
        }

        [Test]
        public void unknown_protocol_is_not_gated()
        {
            Profile().IsProtocolEnabled(DownloadProtocol.Unknown).Should().BeTrue();
        }
    }
}
