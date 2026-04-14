using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using TagLib;

namespace NzbDrone.Core.MediaFiles
{
    public interface IAudioTagService
    {
        ParsedTrackInfo ReadTags(string file);
        void WriteTags(ComicFile comicFile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId);
        List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId);
        void RetagFiles(RetagFilesCommand message);
        void RetagSeries(RetagSeriesCommand message);
    }

    public class AudioTagService : IAudioTagService
    {
        private readonly IConfigService _configService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly ISeriesService _seriesService;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public AudioTagService(IConfigService configService,
                               IMediaFileService mediaFileService,
                               IDiskProvider diskProvider,
                               IRootFolderWatchingService rootFolderWatchingService,
                               ISeriesService seriesService,
                               IMapCoversToLocal mediaCoverService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _configService = configService;
            _mediaFileService = mediaFileService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _seriesService = seriesService;
            _mediaCoverService = mediaCoverService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public AudioTag ReadAudioTag(string path)
        {
            return new AudioTag(path);
        }

        public ParsedTrackInfo ReadTags(string path)
        {
            return new AudioTag(path);
        }

        public AudioTag GetTrackMetadata(ComicFile comicFile)
        {
            var issue = comicFile.Issue?.Value;

            if (issue == null)
            {
                return new AudioTag();
            }

            var series = issue.Series?.Value;
            var partCount = issue.ComicFiles?.Value?.Count ?? 0;

            if (series == null)
            {
                return new AudioTag();
            }

            var fileTags = ReadAudioTag(comicFile.Path);

            string imageFile = null;
            long imageSize = 0;

            if (issue.CoverArtUrl.IsNotNullOrWhiteSpace())
            {
                var coverPath = _mediaCoverService.GetCoverPath(issue.Id, MediaCoverEntity.Issue, MediaCoverTypes.Cover, ".jpg", null);
                _logger.Trace($"Embedding: {coverPath}");
                var fileInfo = _diskProvider.GetFileInfo(coverPath);
                if (fileInfo.Exists)
                {
                    imageFile = coverPath;
                    imageSize = fileInfo.Length;
                }
            }

            return new AudioTag
            {
                Title = issue.Title,
                Performers = new[] { series.Name },
                IssueSeries = new[] { series.Name },
                Track = (uint)comicFile.Part,
                TrackCount = (uint)partCount,
                Issue = issue.Title,
                Disc = fileTags.Disc,
                DiscCount = fileTags.DiscCount,
                Media = fileTags.Media,
                Date = issue.ReleaseDate,
                Year = (uint)(issue.ReleaseDate?.Year ?? 0),
                OriginalReleaseDate = issue.ReleaseDate,
                OriginalYear = (uint)(issue.ReleaseDate?.Year ?? 0),
                Genres = new string[0],
                ImageFile = imageFile,
                ImageSize = imageSize,
            };
        }

        private void UpdateTrackfileSizeAndModified(ComicFile comicFile, string path)
        {
            // update the saved file size so that the importer doesn't get confused on the next scan
            var fileInfo = _diskProvider.GetFileInfo(path);
            comicFile.Size = fileInfo.Length;
            comicFile.Modified = fileInfo.LastWriteTimeUtc;

            if (comicFile.Id > 0)
            {
                _mediaFileService.Update(comicFile);
            }
        }

        public void RemoveAllTags(string path)
        {
            TagLib.File file = null;
            try
            {
                file = TagLib.File.Create(path);
                file.RemoveTags(TagLib.TagTypes.AllTags);
                file.Save();
            }
            catch (CorruptFileException ex)
            {
                _logger.Warn(ex, $"Tag removal failed for {path}.  File is corrupt");
            }
            catch (Exception ex)
            {
                _logger.ForWarnEvent()
                    .Exception(ex)
                    .Message($"Tag removal failed for {path}")
                    .WriteSentryWarn("Tag removal failed")
                    .Log();
            }
            finally
            {
                file?.Dispose();
            }
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
            if (!force)
            {
                if (_configService.WriteAudioTags == WriteAudioTagsType.No ||
                    (_configService.WriteAudioTags == WriteAudioTagsType.NewFiles && !newDownload))
                {
                    return;
                }
            }

            var newTags = GetTrackMetadata(comicFile);
            var path = comicFile.Path;

            var diff = ReadAudioTag(path).Diff(newTags);

            if (!diff.Any())
            {
                _logger.Debug("No tags update for {0} due to no difference", comicFile);
                return;
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(path);

            if (_configService.ScrubAudioTags)
            {
                _logger.Debug($"Scrubbing tags for {comicFile}");
                RemoveAllTags(path);
            }

            _logger.Debug($"Writing tags for {comicFile}");

            newTags.Write(path);

            UpdateTrackfileSizeAndModified(comicFile, path);

            _eventAggregator.PublishEvent(new ComicFileRetaggedEvent(comicFile.Series.Value, comicFile, diff, _configService.ScrubAudioTags));
        }

        public void SyncTags(List<Issue> issues)
        {
            if (_configService.WriteAudioTags != WriteAudioTagsType.Sync)
            {
                return;
            }

            // get the files to update
            foreach (var issue in issues)
            {
                var comicFiles = issue.ComicFiles.Value;

                _logger.Debug($"Syncing audio tags for {comicFiles.Count} files");

                foreach (var file in comicFiles.Where(x => MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(x.Path))))
                {
                    file.Issue = issue;
                    WriteTags(file, false);
                }
            }
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId)
        {
            var files = _mediaFileService.GetFilesBySeries(seriesId);

            return GetPreviews(files).OrderBy(b => b.IssueId).ThenBy(b => b.Path).ToList();
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            var files = _mediaFileService.GetFilesByIssue(issueId);

            return GetPreviews(files).OrderBy(b => b.IssueId).ThenBy(b => b.Path).ToList();
        }

        private IEnumerable<RetagComicFilePreview> GetPreviews(List<ComicFile> files)
        {
            foreach (var f in files.Where(x => MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(x.Path))).OrderBy(x => x.Issue.Value?.Title))
            {
                var file = f;

                if (f.Issue.Value == null)
                {
                    _logger.Warn($"File {f} is not linked to any issues");
                    continue;
                }

                var oldTags = ReadAudioTag(f.Path);
                var newTags = GetTrackMetadata(f);
                var diff = oldTags.Diff(newTags);

                if (diff.Any())
                {
                    yield return new RetagComicFilePreview
                    {
                        SeriesId = file.Series.Value.Id,
                        IssueId = file.IssueId,
                        ComicFileId = file.Id,
                        Path = file.Path,
                        Changes = diff
                    };
                }
            }
        }

        public void RetagFiles(RetagFilesCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var comicFiles = _mediaFileService.Get(message.Files);
            var audioFiles = comicFiles.Where(x => MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(x.Path))).ToList();

            _logger.ProgressInfo("Re-tagging {0} audio files for {1}", audioFiles.Count, series.Name);
            foreach (var file in audioFiles)
            {
                WriteTags(file, false, force: true);
            }

            _logger.ProgressInfo("Selected audio files re-tagged for {0}", series.Name);
        }

        public void RetagSeries(RetagSeriesCommand message)
        {
            _logger.Debug("Re-tagging all audio files for selected allSeries");
            var seriesToRename = _seriesService.GetSeries(message.SeriesIds);

            foreach (var series in seriesToRename)
            {
                var comicFiles = _mediaFileService.GetFilesBySeries(series.Id);
                var audioFiles = comicFiles.Where(x => MediaFileExtensions.AudioExtensions.Contains(Path.GetExtension(x.Path))).ToList();

                _logger.ProgressInfo("Re-tagging all audio files for series: {0}", series.Name);
                foreach (var file in audioFiles)
                {
                    WriteTags(file, false, force: true);
                }

                _logger.ProgressInfo("All audio files re-tagged for {0}", series.Name);
            }
        }
    }
}
