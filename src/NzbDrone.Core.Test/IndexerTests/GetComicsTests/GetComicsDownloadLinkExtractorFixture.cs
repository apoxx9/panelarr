using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.GetComics;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.GetComicsTests
{
    [TestFixture]
    public class GetComicsDownloadLinkExtractorFixture : TestBase
    {
        // Anchor markup trimmed from a real 2026 GetComics post: the main-server
        // button and the mirror buttons all route through getcomics.org/dls/
        // redirects (the /dlds/ path is legacy), while some mirrors also appear
        // as direct external hrefs.
        private const string PostHtml = @"
            <a href=""https://getcomics.org/dls/6iIrVrWM6P3qV2O7nes/abc=="" class=""aio-red"" title=""DOWNLOAD NOW"" rel=""nofollow""><i class=""glyphicons glyphicons-ok""></i>DOWNLOAD NOW</a>
            <a href=""https://getcomics.org/dls/LXzRg4pngXJA/def=="" rel=""nofollow""><span>PIXELDRAIN</span></a>
            <a href=""https://datanodes.to/g8pen7ch0m9g"" rel=""nofollow"">DATANODES</a>
            <a href=""https://vikingfile.com/f/QMfYYH6E2O"" rel=""nofollow"">VIKINGFILE</a>
            <a href=""https://getcomics.org/tag/marvel/"">Marvel</a>";

        private readonly GetComicsDownloadLinkExtractor _extractor = new GetComicsDownloadLinkExtractor();

        [Test]
        public void extracts_the_main_server_download_now_link_as_a_redirect()
        {
            var links = _extractor.ExtractDownloadLinks(PostHtml);

            var main = links.Should().ContainSingle(l => l.Host == GetComicsDownloadHost.MainServer).Subject;
            main.IsRedirect.Should().BeTrue();
            main.Url.Should().StartWith("https://getcomics.org/dls/6iIrVrWM6P3qV2O7nes");
        }

        [Test]
        public void ranks_pixeldrain_then_main_server_ahead_of_captcha_walled_hosts()
        {
            var links = _extractor.ExtractDownloadLinks(PostHtml);

            links.Should().HaveCountGreaterThan(2);
            links[0].Host.Should().Be(GetComicsDownloadHost.Pixeldrain);
            links[1].Host.Should().Be(GetComicsDownloadHost.MainServer);
            links.Should().Contain(l => l.Host == GetComicsDownloadHost.DataNodes);
            links.IndexOf(links.First(l => l.Host == GetComicsDownloadHost.DataNodes))
                 .Should().BeGreaterThan(1);
        }

        [Test]
        public void detects_mirror_hosts_behind_dls_redirects_by_label()
        {
            var links = _extractor.ExtractDownloadLinks(PostHtml);

            var pixeldrain = links.Should().ContainSingle(l => l.Host == GetComicsDownloadHost.Pixeldrain).Subject;
            pixeldrain.IsRedirect.Should().BeTrue();
        }

        [Test]
        public void still_extracts_legacy_dlds_redirect_links()
        {
            var html = @"<a href=""https://getcomics.org/dlds/xyz"" rel=""nofollow"">PIXELDRAIN</a>";

            var links = _extractor.ExtractDownloadLinks(html);

            links.Should().ContainSingle().Which.IsRedirect.Should().BeTrue();
            links[0].Host.Should().Be(GetComicsDownloadHost.Pixeldrain);
        }

        [Test]
        public void ignores_non_download_site_links()
        {
            var links = _extractor.ExtractDownloadLinks(PostHtml);

            links.Should().NotContain(l => l.Url.Contains("/tag/"));
        }

        [Test]
        public void extracts_direct_external_mirror_links()
        {
            var links = _extractor.ExtractDownloadLinks(PostHtml);

            links.Should().Contain(l => l.Host == GetComicsDownloadHost.DataNodes && !l.IsRedirect);
            links.Should().Contain(l => l.Host == GetComicsDownloadHost.Vikingfile && !l.IsRedirect);
        }
    }
}
