using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaCover
{
    public interface IMapCoversToLocal
    {
        void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, IEnumerable<MediaCover> covers);
        string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null);
        void EnsureIssueCovers(Issue issue);
    }

    public class MediaCoverService :
        IHandleAsync<SeriesRefreshCompleteEvent>,
        IHandleAsync<SeriesDeletedEvent>,
        IHandleAsync<IssueDeletedEvent>,
        IMapCoversToLocal
    {
        private const string USER_AGENT = "Dalvik/2.1.0 (Linux; U; Android 10; SM-G975U Build/QP1A.190711.020)";

        private readonly IMediaCoverProxy _mediaCoverProxy;
        private readonly IImageResizer _resizer;
        private readonly IIssueService _issueService;
        private readonly IHttpClient _httpClient;
        private readonly IDiskProvider _diskProvider;
        private readonly ICoverExistsSpecification _coverExistsSpecification;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        private readonly string _coverRootFolder;

        // ImageSharp is slow on ARM (no hardware acceleration on mono yet)
        // So limit the number of concurrent resizing tasks
        private static SemaphoreSlim _semaphore = new SemaphoreSlim((int)Math.Ceiling(Environment.ProcessorCount / 2.0));

        public MediaCoverService(IMediaCoverProxy mediaCoverProxy,
                                 IImageResizer resizer,
                                 IIssueService issueService,
                                 IHttpClient httpClient,
                                 IDiskProvider diskProvider,
                                 IAppFolderInfo appFolderInfo,
                                 ICoverExistsSpecification coverExistsSpecification,
                                 IConfigFileProvider configFileProvider,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _mediaCoverProxy = mediaCoverProxy;
            _resizer = resizer;
            _issueService = issueService;
            _httpClient = httpClient;
            _diskProvider = diskProvider;
            _coverExistsSpecification = coverExistsSpecification;
            _configFileProvider = configFileProvider;
            _eventAggregator = eventAggregator;
            _logger = logger;

            _coverRootFolder = appFolderInfo.GetMediaCoverPath();
        }

        public string GetCoverPath(int entityId, MediaCoverEntity coverEntity, MediaCoverTypes coverType, string extension, int? height = null)
        {
            var heightSuffix = height.HasValue ? "-" + height.ToString() : "";

            if (coverEntity == MediaCoverEntity.Issue)
            {
                return Path.Combine(GetIssueCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
            }

            return Path.Combine(GetSeriesCoverPath(entityId), coverType.ToString().ToLower() + heightSuffix + GetExtension(coverType, extension));
        }

        public void ConvertToLocalUrls(int entityId, MediaCoverEntity coverEntity, IEnumerable<MediaCover> covers)
        {
            if (entityId == 0)
            {
                // Series isn't in Panelarr yet, map via a proxy to circument referrer issues
                foreach (var mediaCover in covers)
                {
                    mediaCover.RemoteUrl = mediaCover.Url;
                    mediaCover.Url = _mediaCoverProxy.RegisterUrl(mediaCover.RemoteUrl);
                }
            }
            else
            {
                foreach (var mediaCover in covers)
                {
                    if (mediaCover.CoverType == MediaCoverTypes.Unknown)
                    {
                        continue;
                    }

                    var filePath = GetCoverPath(entityId, coverEntity, mediaCover.CoverType, mediaCover.Extension, null);

                    mediaCover.RemoteUrl = mediaCover.Url;

                    if (coverEntity == MediaCoverEntity.Issue)
                    {
                        mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/Comics/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                    }
                    else
                    {
                        mediaCover.Url = _configFileProvider.UrlBase + @"/MediaCover/" + entityId + "/" + mediaCover.CoverType.ToString().ToLower() + GetExtension(mediaCover.CoverType, mediaCover.Extension);
                    }

                    if (_diskProvider.FileExists(filePath))
                    {
                        var lastWrite = _diskProvider.FileGetLastWrite(filePath);
                        mediaCover.Url += "?lastWrite=" + lastWrite.Ticks;
                    }
                }
            }
        }

        private string GetSeriesCoverPath(int seriesId)
        {
            return Path.Combine(_coverRootFolder, seriesId.ToString());
        }

        private string GetIssueCoverPath(int issueId)
        {
            return Path.Combine(_coverRootFolder, "Comics", issueId.ToString());
        }

        private void EnsureSeriesCovers(Series series)
        {
            var toResize = new List<Tuple<MediaCover, bool>>();

            foreach (var cover in series.Metadata.Value.Images)
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(series.Id, MediaCoverEntity.Series, cover.CoverType, cover.Extension);
                var alreadyExists = false;

                try
                {
                    // Try the Range header check first for servers that support it
                    try
                    {
                        var serverFileHeaders = GetServerHeaders(cover.Url);
                        alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, GetContentLength(serverFileHeaders), fileName);

                        if (!alreadyExists)
                        {
                            DownloadCover(series, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                        }
                    }
                    catch (Exception)
                    {
                        // Server doesn't support Range header (e.g. ComicVine) — download directly if file doesn't exist
                        if (!_diskProvider.FileExists(fileName) || _diskProvider.GetFileSize(fileName) == 0)
                        {
                            _logger.Debug("Range header check failed for {0}, downloading directly", cover.Url);
                            DownloadCover(series, cover, DateTime.Now);
                        }
                        else
                        {
                            alreadyExists = true;
                        }
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", series, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", series, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", series);
                }

                toResize.Add(Tuple.Create(cover, alreadyExists));
            }

            try
            {
                _semaphore.Wait();

                foreach (var tuple in toResize)
                {
                    EnsureResizedCovers(series, tuple.Item1, !tuple.Item2);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void EnsureIssueCovers(Issue issue)
        {
            var coverImages = new List<MediaCover>();
            if (issue.CoverArtUrl.IsNotNullOrWhiteSpace())
            {
                coverImages.Add(new MediaCover { Url = issue.CoverArtUrl, CoverType = MediaCoverTypes.Cover });
            }

            foreach (var cover in coverImages.Where(e => e.CoverType == MediaCoverTypes.Cover))
            {
                if (cover.CoverType == MediaCoverTypes.Unknown)
                {
                    continue;
                }

                var fileName = GetCoverPath(issue.Id, MediaCoverEntity.Issue, cover.CoverType, cover.Extension, null);
                var alreadyExists = false;

                try
                {
                    try
                    {
                        var serverFileHeaders = GetServerHeaders(cover.Url);
                        alreadyExists = _coverExistsSpecification.AlreadyExists(serverFileHeaders.LastModified, GetContentLength(serverFileHeaders), fileName);

                        if (!alreadyExists)
                        {
                            DownloadIssueCover(issue, cover, serverFileHeaders.LastModified ?? DateTime.Now);
                        }
                    }
                    catch (Exception)
                    {
                        // Server doesn't support Range header — download directly if file doesn't exist
                        if (!_diskProvider.FileExists(fileName) || _diskProvider.GetFileSize(fileName) == 0)
                        {
                            _logger.Debug("Range header check failed for {0}, downloading directly", cover.Url);
                            DownloadIssueCover(issue, cover, DateTime.Now);
                        }
                        else
                        {
                            alreadyExists = true;
                        }
                    }
                }
                catch (HttpException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", issue, e.Message);
                }
                catch (WebException e)
                {
                    _logger.Warn("Couldn't download media cover for {0}. {1}", issue, e.Message);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't download media cover for {0}", issue);
                }
            }
        }

        private void DownloadCover(Series series, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(series.Id, MediaCoverEntity.Series, cover.CoverType, cover.Extension);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, series, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName, USER_AGENT);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for series {1}", cover.CoverType, series);
            }
        }

        private void DownloadIssueCover(Issue issue, MediaCover cover, DateTime lastModified)
        {
            var fileName = GetCoverPath(issue.Id, MediaCoverEntity.Issue, cover.CoverType, cover.Extension, null);

            _logger.Info("Downloading {0} for {1} {2}", cover.CoverType, issue, cover.Url);
            _httpClient.DownloadFile(cover.Url, fileName, USER_AGENT);

            try
            {
                _diskProvider.FileSetLastWriteTime(fileName, lastModified);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Unable to set modified date for {0} image for issue {1}", cover.CoverType, issue);
            }
        }

        private void EnsureResizedCovers(Series series, MediaCover cover, bool forceResize, Issue issue = null)
        {
            var heights = GetDefaultHeights(cover.CoverType);

            foreach (var height in heights)
            {
                var mainFileName = GetCoverPath(series.Id, MediaCoverEntity.Series, cover.CoverType, cover.Extension);
                var resizeFileName = GetCoverPath(series.Id, MediaCoverEntity.Series, cover.CoverType, cover.Extension, height);

                if (forceResize || !_diskProvider.FileExists(resizeFileName) || _diskProvider.GetFileSize(resizeFileName) == 0)
                {
                    _logger.Debug("Resizing {0}-{1} for {2}", cover.CoverType, height, series);

                    try
                    {
                        _resizer.Resize(mainFileName, resizeFileName, height);
                    }
                    catch
                    {
                        _logger.Debug("Couldn't resize media cover {0}-{1} for series {2}, using full size image instead.", cover.CoverType, height, series);
                    }
                }
            }
        }

        private int[] GetDefaultHeights(MediaCoverTypes coverType)
        {
            switch (coverType)
            {
                default:
                    return new int[] { };

                case MediaCoverTypes.Poster:
                case MediaCoverTypes.Disc:
                case MediaCoverTypes.Cover:
                case MediaCoverTypes.Logo:
                case MediaCoverTypes.Headshot:
                    return new[] { 500, 250 };

                case MediaCoverTypes.Banner:
                    return new[] { 70, 35 };

                case MediaCoverTypes.Fanart:
                case MediaCoverTypes.Screenshot:
                    return new[] { 360, 180 };
            }
        }

        private string GetExtension(MediaCoverTypes coverType, string defaultExtension)
        {
            return coverType switch
            {
                MediaCoverTypes.Clearlogo => ".png",
                _ => defaultExtension
            };
        }

        private HttpHeader GetServerHeaders(string url)
        {
            // Some servers don't allow a HEAD, so request a zero byte range instead
            var request = new HttpRequest(url)
            {
                AllowAutoRedirect = true,
            };

            request.Headers.Add("Range", "bytes=0-0");
            request.Headers.Add("User-Agent", USER_AGENT);

            return _httpClient.Get(request).Headers;
        }

        private long? GetContentLength(HttpHeader headers)
        {
            var range = headers.Get("content-range");

            if (range == null)
            {
                return null;
            }

            var split = range.Split('/');
            if (split.Length == 2 && long.TryParse(split[1], out var length))
            {
                return length;
            }

            return null;
        }

        public void HandleAsync(SeriesRefreshCompleteEvent message)
        {
            EnsureSeriesCovers(message.Series);

            var issues = _issueService.GetIssuesBySeries(message.Series.Id);
            foreach (var issue in issues)
            {
                EnsureIssueCovers(issue);
            }

            _eventAggregator.PublishEvent(new MediaCoversUpdatedEvent(message.Series));
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            var path = GetSeriesCoverPath(message.Series.Id);
            if (_diskProvider.FolderExists(path))
            {
                _diskProvider.DeleteFolder(path, true);
            }
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            var path = GetIssueCoverPath(message.Issue.Id);
            if (_diskProvider.FolderExists(path))
            {
                _diskProvider.DeleteFolder(path, true);
            }
        }
    }
}
