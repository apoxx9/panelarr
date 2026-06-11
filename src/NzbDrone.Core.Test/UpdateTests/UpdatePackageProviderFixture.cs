using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.UpdateTests
{
    [TestFixture]
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        private void GivenReleases(string json)
        {
            var rawResponse = new HttpResponse(
                new HttpRequest("https://api.github.com/repos/apoxx9/panelarr/releases"),
                new HttpHeader(),
                Encoding.UTF8.GetBytes(json),
                HttpStatusCode.OK);

            Mocker.GetMock<ICachedHttpResponseService>()
                  .Setup(s => s.Get<List<GitHubRelease>>(It.IsAny<HttpRequest>(), It.IsAny<bool>(), It.IsAny<TimeSpan>()))
                  .Returns(new HttpResponse<List<GitHubRelease>>(rawResponse));
        }

        private static string ReleaseJson(string tag, bool draft = false, bool prerelease = false, string body = null, string assetName = null)
        {
            var release = new Dictionary<string, object>
            {
                ["tag_name"] = tag,
                ["name"] = tag,
                ["body"] = body,
                ["html_url"] = $"https://github.com/apoxx9/panelarr/releases/tag/{tag}",
                ["published_at"] = "2026-06-01T12:00:00Z",
                ["draft"] = draft,
                ["prerelease"] = prerelease,
                ["assets"] = assetName == null
                    ? Array.Empty<object>()
                    : new object[] { new Dictionary<string, object> { ["name"] = assetName, ["browser_download_url"] = $"https://github.com/apoxx9/panelarr/releases/download/{tag}/{assetName}" } }
            };

            return JsonConvert.SerializeObject(release);
        }

        [Test]
        public void should_map_releases_to_update_packages()
        {
            GivenReleases($"[{ReleaseJson("v1.1.0", body: "### New\n- Publisher UI\n### Fixed\n- Wizard saves settings")}]");

            var updates = Subject.GetRecentUpdates("main", new Version(1, 0, 0));

            updates.Should().HaveCount(1);
            updates[0].Version.Should().Be(new Version(1, 1, 0));
            updates[0].Branch.Should().Be("main");
            updates[0].Url.Should().Contain("/releases/tag/v1.1.0");
            updates[0].FileName.Should().BeNull();
            updates[0].Changes.New.Should().ContainSingle().Which.Should().Be("Publisher UI");
            updates[0].Changes.Fixed.Should().ContainSingle().Which.Should().Be("Wizard saves settings");
        }

        [Test]
        public void should_skip_drafts_prereleases_and_unparsable_tags()
        {
            GivenReleases($"[{ReleaseJson("v1.2.0", draft: true)},{ReleaseJson("v1.1.5", prerelease: true)},{ReleaseJson("nightly")},{ReleaseJson("v1.1.0")}]");

            var updates = Subject.GetRecentUpdates("main", new Version(1, 0, 0));

            updates.Should().HaveCount(1);
            updates[0].Version.Should().Be(new Version(1, 1, 0));
        }

        [Test]
        public void latest_update_should_be_newest_version_above_current()
        {
            GivenReleases($"[{ReleaseJson("v1.1.0")},{ReleaseJson("v1.2.0")},{ReleaseJson("v1.0.0")}]");

            var update = Subject.GetLatestUpdate("main", new Version(1, 1, 0));

            update.Should().NotBeNull();
            update.Version.Should().Be(new Version(1, 2, 0));
        }

        [Test]
        public void no_update_when_current_version_is_newest()
        {
            GivenReleases($"[{ReleaseJson("v1.1.0")},{ReleaseJson("v1.0.0")}]");

            Subject.GetLatestUpdate("main", new Version(1, 1, 0)).Should().BeNull();
        }

        [Test]
        public void should_return_empty_when_github_is_unreachable()
        {
            Mocker.GetMock<ICachedHttpResponseService>()
                  .Setup(s => s.Get<List<GitHubRelease>>(It.IsAny<HttpRequest>(), It.IsAny<bool>(), It.IsAny<TimeSpan>()))
                  .Throws(new HttpException(new HttpResponse(new HttpRequest("https://api.github.com"), new HttpHeader(), Array.Empty<byte>(), HttpStatusCode.ServiceUnavailable)));

            Subject.GetRecentUpdates("main", new Version(1, 0, 0)).Should().BeEmpty();
            Subject.GetLatestUpdate("main", new Version(1, 0, 0)).Should().BeNull();
            ExceptionVerification.ExpectedWarns(2);
        }

        [Test]
        public void should_use_platform_asset_when_release_has_one()
        {
            var os = NzbDrone.Common.EnvironmentInfo.OsInfo.Os.ToString().ToLowerInvariant();

            GivenReleases($"[{ReleaseJson("v1.1.0", assetName: $"Panelarr.1.1.0.{os}-x64.tar.gz")}]");

            var updates = Subject.GetRecentUpdates("main", new Version(1, 0, 0));

            updates.Single().FileName.Should().NotBeNull();
            updates.Single().Url.Should().Contain("/releases/download/");
        }

        [Test]
        public void bullets_without_headings_should_count_as_new()
        {
            GivenReleases($"[{ReleaseJson("v1.1.0", body: "- first change\n- second change")}]");

            var updates = Subject.GetRecentUpdates("main", new Version(1, 0, 0));

            updates.Single().Changes.New.Should().HaveCount(2);
            updates.Single().Changes.Fixed.Should().BeEmpty();
        }

        [Test]
        public void empty_body_should_map_to_null_changes()
        {
            GivenReleases($"[{ReleaseJson("v1.1.0")}]");

            Subject.GetRecentUpdates("main", new Version(1, 0, 0)).Single().Changes.Should().BeNull();
        }
    }
}
