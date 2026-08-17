using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.GetComics;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.GetComics;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.GetComicsTests
{
    [TestFixture]
    public class GetComicsDownloadClientFixture : DownloadClientFixtureBase<GetComicsDownloadClient>
    {
        private string _downloadFolder;
        private string _parentFolder;

        [SetUp]
        public void Setup()
        {
            _parentFolder = @"c:\downloads".AsOsAgnostic();
            _downloadFolder = @"c:\downloads\getcomics".AsOsAgnostic();

            Subject.Definition = new DownloadClientDefinition
            {
                Settings = new GetComicsDownloadClientSettings
                {
                    DownloadFolder = _downloadFolder
                }
            };
        }

        private void GivenFolderExists(string folder, bool exists)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(folder))
                  .Returns(exists);
        }

        [Test]
        public void test_should_create_missing_download_folder_when_parent_exists()
        {
            GivenFolderExists(_downloadFolder, false);
            GivenFolderExists(_parentFolder, true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_downloadFolder))
                  .Returns(_parentFolder);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(_downloadFolder), Times.Once());
        }

        [Test]
        public void test_should_not_create_folder_when_parent_missing()
        {
            GivenFolderExists(_downloadFolder, false);
            GivenFolderExists(_parentFolder, false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_downloadFolder))
                  .Returns(_parentFolder);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void test_should_not_create_folder_when_it_already_exists()
        {
            GivenFolderExists(_downloadFolder, true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderWritable(_downloadFolder))
                  .Returns(true);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(It.IsAny<string>()), Times.Never());
        }

        // The hidden-form and JSON fragments below are trimmed from real
        // datanodes.to responses captured while reverse-engineering the flow.
        [Test]
        public void extract_hidden_form_value_reads_the_datanodes_file_id()
        {
            var html = @"<form method=""POST"" action='' id=""downloadForm"">
                <input type=""hidden"" name=""op"" value=""download1"">
                <input type=""hidden"" name=""id"" value=""egrh5qbb4gq8"">
                <input type=""hidden"" name=""fname"" value=""Something is Killing the Children 047 &#40;2026&#41;.cbz"">
                </form>";

            GetComicsDownloadClient.ExtractHiddenFormValue(html, "id").Should().Be("egrh5qbb4gq8");
        }

        [Test]
        public void extract_hidden_form_value_html_decodes_the_filename()
        {
            var html = @"<input type=""hidden"" name=""fname"" value=""Comic &#40;2026&#41;.cbz"">";

            GetComicsDownloadClient.ExtractHiddenFormValue(html, "fname").Should().Be("Comic (2026).cbz");
        }

        [Test]
        public void extract_hidden_form_value_returns_null_when_absent()
        {
            GetComicsDownloadClient.ExtractHiddenFormValue("<html></html>", "id").Should().BeNull();
        }

        [Test]
        public void extract_datanodes_json_url_decodes_the_tunnel_url()
        {
            var json = @"{""url"":""https%3A%2F%2Ftunnel5.dlproxy.uk%2Fdownload%2FabcDEF123""}";

            GetComicsDownloadClient.ExtractDataNodesJsonUrl(json)
                .Should().Be("https://tunnel5.dlproxy.uk/download/abcDEF123");
        }

        [Test]
        public void extract_datanodes_json_url_returns_null_on_error_body()
        {
            var json = @"{""error"":""Please wait""}";

            GetComicsDownloadClient.ExtractDataNodesJsonUrl(json).Should().BeNull();
        }

        private void GivenExtractedLinks(params GetComicsDownloadLink[] links)
        {
            Mocker.GetMock<IGetComicsDownloadLinkExtractor>()
                  .Setup(s => s.ExtractDownloadLinks(It.IsAny<string>()))
                  .Returns(new List<GetComicsDownloadLink>(links));

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.GetAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), "<html></html>")));
        }

        private static GetComicsDownloadLink PixeldrainLink(string id)
        {
            return new GetComicsDownloadLink
            {
                Url = $"https://pixeldrain.com/u/{id}",
                Host = GetComicsDownloadHost.Pixeldrain,
                Label = "PixelDrain",
            };
        }

        [Test]
        public void datanodes_download2_echoes_the_rand_token_from_the_countdown_page()
        {
            GetComicsDownloadClient.DataNodesCountdown = TimeSpan.Zero;

            GivenExtractedLinks(new GetComicsDownloadLink
            {
                Url = "https://datanodes.to/abc123/file.cbz",
                Host = GetComicsDownloadHost.DataNodes,
                Label = "DATANODES",
            });

            var filePageHtml = @"<form><input type=""hidden"" name=""id"" value=""abc123""><input type=""hidden"" name=""fname"" value=""file.cbz""></form>";

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.GetAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => Task.FromResult(new HttpResponse(r, new HttpHeader(), filePageHtml)));

            var posts = new List<string>();
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.PostAsync(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r =>
                  {
                      var body = r.ContentData != null ? System.Text.Encoding.UTF8.GetString(r.ContentData) : string.Empty;
                      posts.Add(body);

                      // download1 -> countdown page carrying the one-time token
                      var countdownHtml = @"<form><input type=""hidden"" name=""rand"" value=""r4ndT0ken""></form>";
                      var downloadJson = @"{""url"":""https%3A%2F%2Ftunnel.dlproxy.uk%2Fdl%2Fzzz""}";
                      var content = body.Contains("op=download1") ? countdownHtml : downloadJson;

                      return Task.FromResult(new HttpResponse(r, new HttpHeader(), content));
                  });

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

            var id = Subject.Download(CreateRemoteIssue(), CreateIndexer()).GetAwaiter().GetResult();

            id.Should().NotBeNull();
            posts.Should().HaveCount(2);
            posts[1].Should().Contain("op=download2");
            posts[1].Should().Contain("rand=r4ndT0ken");
        }

        [Test]
        public void main_server_redirect_resolves_to_the_file_it_lands_on()
        {
            GivenExtractedLinks(new GetComicsDownloadLink
            {
                Url = "https://getcomics.org/dls/6iIrVrWM6P3qV2O7nes/abc==",
                Host = GetComicsDownloadHost.MainServer,
                Label = "DOWNLOAD NOW",
                IsRedirect = true,
            });

            var fileUrl = "https://fs3.comicfiles.ru/2026.08.12/The%20Walking%20Dead%20Deluxe%20139%20(2026).cbz";

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.GetAsync(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/dls/"))))
                  .Returns<HttpRequest>(r =>
                  {
                      var headers = new HttpHeader { { "Location", fileUrl } };
                      return Task.FromResult(new HttpResponse(r, headers, string.Empty, System.Net.HttpStatusCode.Found));
                  });

            string downloadedUrl = null;
            string downloadedPath = null;
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns<string, string, string>((url, file, ua) =>
                  {
                      downloadedUrl = url;
                      downloadedPath = file;
                      return Task.CompletedTask;
                  });

            var id = Subject.Download(CreateRemoteIssue(), CreateIndexer()).GetAwaiter().GetResult();

            id.Should().NotBeNull();
            downloadedUrl.Should().Be(fileUrl);
            downloadedPath.Should().EndWith(".cbz");
        }

        [Test]
        public void download_falls_back_to_next_mirror_when_first_download_fails()
        {
            GivenExtractedLinks(PixeldrainLink("aaa"), PixeldrainLink("bbb"));

            var attempts = 0;
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns<string, string, string>((url, file, ua) =>
                  {
                      attempts++;
                      if (attempts == 1)
                      {
                          throw new Exception("Site responded with html content.");
                      }

                      return Task.CompletedTask;
                  });

            var id = Subject.Download(CreateRemoteIssue(), CreateIndexer()).GetAwaiter().GetResult();

            id.Should().NotBeNull();
            attempts.Should().Be(2);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void download_throws_when_all_hosts_are_unsupported()
        {
            GivenExtractedLinks(new GetComicsDownloadLink
            {
                Url = "https://mega.nz/file/abc",
                Host = GetComicsDownloadHost.Mega,
                Label = "MEGA",
            });

            Assert.ThrowsAsync<ReleaseDownloadException>(
                () => Subject.Download(CreateRemoteIssue(), CreateIndexer()));

            Mocker.GetMock<IHttpClient>()
                  .Verify(s => s.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
