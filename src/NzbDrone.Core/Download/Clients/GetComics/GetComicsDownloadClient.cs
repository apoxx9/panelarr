using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.GetComics;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;

namespace NzbDrone.Core.Download.Clients.GetComics
{
    public class GetComicsDownloadClient : DownloadClientBase<GetComicsDownloadClientSettings>
    {
        private readonly IHttpClient _httpClient;
        private readonly IGetComicsDownloadLinkExtractor _linkExtractor;
        private readonly ICached<GetComicsDownloadItem> _downloadCache;

        public override string Name => "GetComics Direct Download";

        public override DownloadProtocol Protocol => DownloadProtocol.DirectDownload;

        public GetComicsDownloadClient(
            IHttpClient httpClient,
            IGetComicsDownloadLinkExtractor linkExtractor,
            ICacheManager cacheManager,
            IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            Logger logger)
            : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _httpClient = httpClient;
            _linkExtractor = linkExtractor;
            _downloadCache = cacheManager.GetCache<GetComicsDownloadItem>(GetType());
        }

        public override async Task<string> Download(RemoteIssue remoteIssue, IIndexer indexer)
        {
            var postPageUrl = remoteIssue.Release.DownloadUrl;
            var title = remoteIssue.Release.Title;
            var cleanTitle = FileNameBuilder.CleanFileName(title);

            _logger.Info("GetComics: Resolving download links from post page: {0}", postPageUrl);

            // Step 1: Fetch the post page HTML
            string postPageHtml;
            try
            {
                var request = new HttpRequest(postPageUrl)
                {
                    AllowAutoRedirect = true
                };
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.GetAsync(request);
                postPageHtml = response.Content;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetComics: Failed to fetch post page: {0}", postPageUrl);
                throw new ReleaseDownloadException(remoteIssue.Release, "Failed to fetch GetComics post page", ex);
            }

            // Step 2: Extract download links
            var downloadLinks = _linkExtractor.ExtractDownloadLinks(postPageHtml);

            if (!downloadLinks.Any())
            {
                _logger.Warn("GetComics: No download links found on post page: {0}", postPageUrl);
                throw new ReleaseDownloadException(remoteIssue.Release, "No download links found on GetComics post page");
            }

            // Step 3: Try to download from the best available link
            var downloadUrl = await ResolveDownloadUrl(downloadLinks);

            if (downloadUrl == null)
            {
                var availableHosts = string.Join(", ", downloadLinks.Select(l => l.Host.ToString()));
                _logger.Warn("GetComics: Could not resolve any downloadable URL. Available hosts: {0}", availableHosts);
                throw new ReleaseDownloadException(remoteIssue.Release, $"Could not resolve a direct download URL. Available hosts: {availableHosts}");
            }

            // Transform host-specific URLs to direct download URLs
            downloadUrl = TransformToDirectDownloadUrl(downloadUrl);

            _logger.Info("GetComics: Downloading from resolved URL: {0}", downloadUrl);

            // Step 4: Download the file
            var downloadFolder = Settings.DownloadFolder;
            var fileName = cleanTitle + GetFileExtension(downloadUrl, ".cbz");
            var filePath = Path.Combine(downloadFolder, fileName);

            try
            {
                await _httpClient.DownloadFileAsync(downloadUrl, filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetComics: Failed to download file from {0}", downloadUrl);
                throw new ReleaseDownloadException(remoteIssue.Release, "Failed to download comic file", ex);
            }

            _logger.Info("GetComics: Successfully downloaded '{0}' to '{1}'", title, filePath);

            // Track the download in our cache for GetItems()
            var downloadId = Definition.Name + "_" + HashConverter.GetHash(filePath).ToHexString();
            var cacheItem = new GetComicsDownloadItem
            {
                Title = title,
                FilePath = filePath,
                DownloadedAt = DateTime.UtcNow,
            };

            _downloadCache.Set(downloadId, cacheItem, TimeSpan.FromDays(1));

            return downloadId;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var downloadFolder = Settings.DownloadFolder;

            if (downloadFolder.IsNullOrWhiteSpace() || !_diskProvider.FolderExists(downloadFolder))
            {
                yield break;
            }

            // Report files present in the download folder
            foreach (var file in _diskProvider.GetFiles(downloadFolder, false))
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension != ".cbz" && extension != ".cbr" && extension != ".pdf" && extension != ".zip" && extension != ".rar")
                {
                    continue;
                }

                var title = FileNameBuilder.CleanFileName(Path.GetFileNameWithoutExtension(file));
                var fileSize = _diskProvider.GetFileSize(file);
                var downloadId = Definition.Name + "_" + HashConverter.GetHash(file).ToHexString();

                var item = new DownloadClientItem
                {
                    DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false),
                    DownloadId = downloadId,
                    Category = "Panelarr",
                    Title = title,
                    TotalSize = fileSize,
                    RemainingTime = TimeSpan.Zero,
                    OutputPath = new OsPath(file),
                    Status = _diskProvider.IsFileLocked(file)
                        ? DownloadItemStatus.Downloading
                        : DownloadItemStatus.Completed,
                    CanBeRemoved = true,
                    CanMoveFiles = true,
                };

                yield return item;
            }
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (!deleteData)
            {
                throw new NotSupportedException("GetComics Direct Download cannot remove items without deleting data.");
            }

            DeleteItemData(item);
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = true,
                OutputRootFolders = new List<OsPath> { new OsPath(Settings.DownloadFolder) }
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestFolder(Settings.DownloadFolder, "DownloadFolder"));
        }

        /// <summary>
        /// Resolves a final downloadable URL from the list of extracted links.
        /// Tries each link in priority order. For getcomics.org/dlds/ redirects,
        /// follows the redirect to get the actual host URL.
        /// </summary>
        private async Task<string> ResolveDownloadUrl(List<GetComicsDownloadLink> links)
        {
            foreach (var link in links)
            {
                try
                {
                    var url = link.Url;

                    // For redirect links, resolve to the actual URL
                    if (link.IsRedirect)
                    {
                        url = await FollowRedirect(url);

                        if (url == null)
                        {
                            _logger.Debug("GetComics: Redirect resolution failed for {0} ({1})", link.Label, link.Url);
                            continue;
                        }

                        _logger.Debug("GetComics: Redirect resolved {0} -> {1}", link.Label, url);
                    }

                    // Check if the resolved URL is from a directly-downloadable host
                    if (IsDirectlyDownloadable(url))
                    {
                        return url;
                    }

                    _logger.Debug("GetComics: Host not directly downloadable, skipping: {0} ({1})", link.Label, url);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "GetComics: Error resolving link {0}", link.Label);
                }
            }

            return null;
        }

        /// <summary>
        /// Follows a redirect URL (e.g., getcomics.org/dlds/...) and returns the final destination URL.
        /// </summary>
        private async Task<string> FollowRedirect(string url)
        {
            try
            {
                var request = new HttpRequest(url)
                {
                    AllowAutoRedirect = false,
                };
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                var response = await _httpClient.GetAsync(request);

                if (response.StatusCode == HttpStatusCode.Moved ||
                    response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.TemporaryRedirect ||
                    response.StatusCode == HttpStatusCode.PermanentRedirect)
                {
                    var location = response.Headers.GetSingleValue("Location");

                    if (location.IsNotNullOrWhiteSpace())
                    {
                        return location;
                    }
                }

                _logger.Debug("GetComics: Expected redirect but got status {0} for {1}", response.StatusCode, url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "GetComics: Failed to follow redirect for {0}", url);
                return null;
            }
        }

        /// <summary>
        /// Determines if a URL points to a host where we can directly download files
        /// via a simple HTTP GET (following redirects).
        /// </summary>
        private static bool IsDirectlyDownloadable(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            var uri = url.ToLowerInvariant();

            // These hosts serve files directly via HTTP
            if (uri.Contains("pixeldrain.com"))
            {
                return true;
            }

            if (uri.Contains("datanodes.to"))
            {
                return true;
            }

            if (uri.Contains("vikingfile.com"))
            {
                return true;
            }

            if (uri.Contains("fileq.net"))
            {
                return true;
            }

            if (uri.Contains("rootz.so"))
            {
                return true;
            }

            // These hosts require special handling / browser interaction -- not directly downloadable
            // mega.nz, mediafire.com, drive.google.com, terabox.com
            return false;
        }

        /// <summary>
        /// Tries to determine a file extension from the URL, or returns a default.
        /// </summary>
        private static string GetFileExtension(string url, string defaultExtension)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var ext = Path.GetExtension(path);

                if (ext.IsNotNullOrWhiteSpace() &&
                    (ext.Equals(".cbz", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".cbr", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".zip", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".rar", StringComparison.OrdinalIgnoreCase)))
                {
                    return ext;
                }
            }
            catch
            {
                // URL parsing failed, use default
            }

            return defaultExtension;
        }

        /// <summary>
        /// Transforms a file-hosting page URL into a direct download API URL where applicable.
        /// For example, Pixeldrain viewer URLs become API download URLs.
        /// </summary>
        private static string TransformToDirectDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return url;
            }

            // Pixeldrain: /u/{id} -> /api/file/{id}?download
            if (url.Contains("pixeldrain.com/u/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(url);
                    var segments = uri.AbsolutePath.TrimEnd('/').Split('/');
                    var fileId = segments[^1];

                    if (fileId.IsNotNullOrWhiteSpace())
                    {
                        return $"https://pixeldrain.com/api/file/{fileId}?download";
                    }
                }
                catch
                {
                    // Fall through to return original URL
                }
            }

            return url;
        }

        private class GetComicsDownloadItem
        {
            public string Title { get; set; }
            public string FilePath { get; set; }
            public DateTime DownloadedAt { get; set; }
        }
    }
}
