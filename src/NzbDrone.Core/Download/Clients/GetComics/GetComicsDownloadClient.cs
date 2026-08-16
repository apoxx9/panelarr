using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
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
        private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        // DataNodes enforces a ~5s countdown before op=download2 is honoured.
        // internal-settable so tests don't wait out the real countdown
        internal static TimeSpan DataNodesCountdown = TimeSpan.FromSeconds(6);

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
                request.Headers.Add("User-Agent", BrowserUserAgent);

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

            // Step 3: Try each mirror in priority order until one yields a real
            // file. A single dead mirror (host down, HTML error page, captcha
            // wall) must not fail the grab when the post lists other mirrors.
            var downloadFolder = Settings.DownloadFolder;
            string filePath = null;
            var attempted = new List<string>();

            foreach (var link in downloadLinks)
            {
                string resolvedUrl;
                try
                {
                    resolvedUrl = await ResolveMirrorUrl(link);
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "GetComics: Failed to resolve {0} mirror, trying next", link.Host);
                    continue;
                }

                if (resolvedUrl == null)
                {
                    continue;
                }

                attempted.Add(link.Host.ToString());
                var candidatePath = Path.Combine(downloadFolder, cleanTitle + GetFileExtension(resolvedUrl, ".cbz"));

                try
                {
                    _logger.Info("GetComics: Downloading from {0} mirror: {1}", link.Host, resolvedUrl);
                    await _httpClient.DownloadFileAsync(resolvedUrl, candidatePath);
                    filePath = candidatePath;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "GetComics: Download from {0} mirror failed, trying next mirror", link.Host);
                    if (_diskProvider.FileExists(candidatePath))
                    {
                        _diskProvider.DeleteFile(candidatePath);
                    }
                }
            }

            if (filePath == null)
            {
                var hosts = attempted.Any()
                    ? string.Join(", ", attempted)
                    : string.Join(", ", downloadLinks.Select(l => l.Host.ToString()));
                _logger.Warn("GetComics: All mirrors failed for '{0}'. Attempted: {1}", title, hosts);
                throw new ReleaseDownloadException(remoteIssue.Release, $"All GetComics mirrors failed. Hosts: {hosts}");
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
            EnsureDownloadFolder();
            failures.AddIfNotNull(TestFolder(Settings.DownloadFolder, "DownloadFolder"));
        }

        // A fresh install has no download folder yet; create it when the
        // parent exists (e.g. a mounted /downloads volume) so setup does not
        // dead-end on "folder does not exist". A missing parent still fails
        // validation — silently creating deep paths inside a container's
        // writable layer would hide data on container recreation.
        private void EnsureDownloadFolder()
        {
            var folder = Settings.DownloadFolder;

            if (folder.IsNullOrWhiteSpace() || _diskProvider.FolderExists(folder))
            {
                return;
            }

            var parent = _diskProvider.GetParentFolder(folder);

            if (parent.IsNullOrWhiteSpace() || !_diskProvider.FolderExists(parent))
            {
                return;
            }

            try
            {
                _diskProvider.CreateFolder(folder);
                _logger.Info("Created GetComics download folder {0}", folder);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Could not create GetComics download folder {0}", folder);
            }
        }

        /// <summary>
        /// Resolves a single extracted link into a directly-downloadable URL, or
        /// null if this host cannot be automated (captcha / browser-only). Handles
        /// the getcomics.org/dlds/ redirect and per-host resolution.
        /// </summary>
        private async Task<string> ResolveMirrorUrl(GetComicsDownloadLink link)
        {
            var url = link.Url;

            if (link.IsRedirect)
            {
                url = await FollowRedirect(url);

                if (url == null)
                {
                    _logger.Debug("GetComics: Redirect resolution failed for {0} ({1})", link.Label, link.Url);
                    return null;
                }

                _logger.Debug("GetComics: Redirect resolved {0} -> {1}", link.Label, url);
            }

            // Pixeldrain serves files via a simple API URL transform.
            if (url.Contains("pixeldrain.com", StringComparison.OrdinalIgnoreCase))
            {
                return TransformToDirectDownloadUrl(url);
            }

            // DataNodes is an XFS host: two-step form POST yields a JSON tunnel URL.
            if (url.Contains("datanodes.to", StringComparison.OrdinalIgnoreCase))
            {
                return await ResolveDataNodesUrl(url);
            }

            // Everything else (mega, mediafire, google drive, vikingfile, fileq,
            // rootz, terabox) needs captcha/browser interaction we can't automate.
            _logger.Debug("GetComics: Host not automatable, skipping: {0}", url);
            return null;
        }

        /// <summary>
        /// Resolves a DataNodes file-page URL to a direct download URL by walking
        /// its XFS two-step flow: GET the page to read the hidden form, POST
        /// op=download1 to reach the countdown page, wait out the server-side
        /// countdown, then POST op=download2 which returns JSON {url}. The session
        /// cookie set on the first response is persisted (StoreResponseCookie) so
        /// the second POST is recognised.
        /// </summary>
        private async Task<string> ResolveDataNodesUrl(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            var postUrl = $"{uri.Scheme}://{uri.Host}/download";

            var pageRequest = new HttpRequest(fileUrl) { AllowAutoRedirect = true };
            pageRequest.Headers.Add("User-Agent", BrowserUserAgent);
            pageRequest.StoreResponseCookie = true;
            var pageResponse = await _httpClient.GetAsync(pageRequest);

            var id = ExtractHiddenFormValue(pageResponse.Content, "id");
            if (id.IsNullOrWhiteSpace())
            {
                _logger.Debug("GetComics: DataNodes page had no download form id: {0}", fileUrl);
                return null;
            }

            var fname = ExtractHiddenFormValue(pageResponse.Content, "fname") ?? string.Empty;

            var step1 = BuildDataNodesPost(postUrl, new Dictionary<string, string>
            {
                { "op", "download1" },
                { "usr_login", string.Empty },
                { "id", id },
                { "fname", fname },
                { "referer", string.Empty },
                { "method_free", "Free Download >>" },
            });
            var step1Response = await _httpClient.PostAsync(step1);

            // The countdown page's form carries a one-time rand token that
            // download2 must echo back - without it the server returns the
            // plain file page instead of the download JSON.
            var rand = ExtractHiddenFormValue(step1Response.Content, "rand") ?? string.Empty;

            if (rand.IsNullOrWhiteSpace())
            {
                _logger.Debug("GetComics: DataNodes countdown page had no rand token for {0}", fileUrl);
            }

            // The countdown is server-enforced; posting download2 early is rejected.
            await Task.Delay(DataNodesCountdown);

            var step2 = BuildDataNodesPost(postUrl, new Dictionary<string, string>
            {
                { "op", "download2" },
                { "id", id },
                { "rand", rand },
                { "referer", string.Empty },
                { "method_free", "Free Download >>" },
                { "method_premium", string.Empty },
                { "g_captch__a", "1" },
            });
            step2.Headers.Accept = "application/json";
            var jsonResponse = await _httpClient.PostAsync(step2);

            var directUrl = ExtractDataNodesJsonUrl(jsonResponse.Content);
            if (directUrl.IsNullOrWhiteSpace())
            {
                _logger.Debug("GetComics: DataNodes download2 returned no url. Body: {0}", jsonResponse.Content);
                return null;
            }

            return directUrl;
        }

        private HttpRequest BuildDataNodesPost(string postUrl, Dictionary<string, string> form)
        {
            var builder = new HttpRequestBuilder(postUrl).Post();
            builder.AllowAutoRedirect = false;

            foreach (var pair in form)
            {
                builder.AddFormParameter(pair.Key, pair.Value);
            }

            var request = builder.Build();
            request.Headers.Add("User-Agent", BrowserUserAgent);
            request.StoreRequestCookie = true;
            request.StoreResponseCookie = true;
            request.SuppressHttpError = true;
            return request;
        }

        internal static string ExtractHiddenFormValue(string html, string name)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var match = Regex.Match(
                html,
                $@"name=""{Regex.Escape(name)}""\s+value=""([^""]*)""",
                RegexOptions.IgnoreCase);

            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
        }

        internal static string ExtractDataNodesJsonUrl(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var match = Regex.Match(json, @"""url""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase);
            return match.Success ? WebUtility.UrlDecode(match.Groups[1].Value) : null;
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
                request.Headers.Add("User-Agent", BrowserUserAgent);

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
