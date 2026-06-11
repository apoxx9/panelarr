using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Http;

namespace NzbDrone.Core.Update
{
    public interface IUpdatePackageProvider
    {
        UpdatePackage GetLatestUpdate(string branch, Version currentVersion);
        List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null);
    }

    // Update information comes from GitHub releases (there is no panelarr
    // cloud service). This is a check-only provider: releases without a
    // platform package asset can be surfaced but not auto-installed.
    public class UpdatePackageProvider : IUpdatePackageProvider
    {
        private const string ReleasesUrl = "https://api.github.com/repos/apoxx9/panelarr/releases?per_page=20";

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;

        public UpdatePackageProvider(ICachedHttpResponseService cachedHttpClient, Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _logger = logger;
        }

        public UpdatePackage GetLatestUpdate(string branch, Version currentVersion)
        {
            return GetRecentUpdates(branch, currentVersion)
                .Where(p => p.Version > currentVersion)
                .OrderByDescending(p => p.Version)
                .FirstOrDefault();
        }

        public List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null)
        {
            try
            {
                var request = new HttpRequest(ReleasesUrl);
                request.Headers.Accept = "application/vnd.github+json";

                var releases = _cachedHttpClient.Get<List<GitHubRelease>>(request, true, CacheDuration).Resource;

                return releases.Where(r => !r.Draft && !r.Prerelease)
                               .Select(r => MapRelease(r, branch))
                               .Where(p => p != null)
                               .OrderByDescending(p => p.Version)
                               .ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to fetch update information from GitHub");
                return new List<UpdatePackage>();
            }
        }

        private UpdatePackage MapRelease(GitHubRelease release, string branch)
        {
            var tag = release.TagName?.TrimStart('v', 'V');

            if (tag.IsNullOrWhiteSpace() || !Version.TryParse(tag, out var version))
            {
                _logger.Debug("Skipping GitHub release with unparsable tag '{0}'", release.TagName);
                return null;
            }

            var asset = FindPackageAsset(release);

            return new UpdatePackage
            {
                Version = version,
                ReleaseDate = release.PublishedAt ?? DateTime.UtcNow,
                Branch = branch,
                Url = asset?.BrowserDownloadUrl ?? release.HtmlUrl,
                FileName = asset?.Name,
                Hash = null,
                Changes = ParseChanges(release.Body)
            };
        }

        // A future packaged release would attach per-OS archives; until then
        // releases carry no installable asset and FileName stays null.
        private static GitHubReleaseAsset FindPackageAsset(GitHubRelease release)
        {
            if (release.Assets == null)
            {
                return null;
            }

            var os = OsInfo.Os.ToString().ToLowerInvariant();

            return release.Assets.FirstOrDefault(a => a.Name != null &&
                                                      a.Name.ToLowerInvariant().Contains(os) &&
                                                      (a.Name.EndsWith(".zip") || a.Name.EndsWith(".tar.gz")));
        }

        // Buckets markdown bullet lines under New/Fixed using any
        // "New|Features|Added|Changes" / "Fix..." headings; bullets before
        // any heading count as New.
        private static UpdateChanges ParseChanges(string body)
        {
            if (body.IsNullOrWhiteSpace())
            {
                return null;
            }

            var changes = new UpdateChanges();
            var current = changes.New;

            foreach (var rawLine in body.Split('\n'))
            {
                var line = rawLine.Trim();

                if (line.StartsWith("#"))
                {
                    var heading = line.TrimStart('#').Trim().ToLowerInvariant();

                    if (heading.Contains("fix"))
                    {
                        current = changes.Fixed;
                    }
                    else if (heading.Contains("new") || heading.Contains("feature") || heading.Contains("added") || heading.Contains("change"))
                    {
                        current = changes.New;
                    }

                    continue;
                }

                if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    current.Add(line.Substring(2).Trim());
                }
            }

            if (!changes.New.Any() && !changes.Fixed.Any())
            {
                return null;
            }

            return changes;
        }
    }
}
