using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileService
    {
        ComicFile Add(ComicFile comicFile);
        void AddMany(List<ComicFile> comicFiles);
        void Update(ComicFile comicFile);
        void Update(List<ComicFile> comicFiles);
        void Delete(ComicFile comicFile, DeleteMediaFileReason reason);
        void DeleteMany(List<ComicFile> comicFiles, DeleteMediaFileReason reason);
        List<ComicFile> GetFilesBySeries(int seriesId);
        List<ComicFile> GetFilesBySeriesMetadataId(int seriesMetadataId);
        List<ComicFile> GetFilesByIssue(int issueId);
        List<ComicFile> GetUnmappedFiles();
        List<IFileInfo> FilterUnchangedFiles(List<IFileInfo> files, FilterFilesType filter);
        ComicFile Get(int id);
        List<ComicFile> Get(IEnumerable<int> ids);
        List<ComicFile> GetFilesWithBasePath(string path);
        List<ComicFile> GetFileWithPath(List<string> path);
        ComicFile GetFileWithPath(string path);
        void UpdateMediaInfo(List<ComicFile> comicFiles);
    }

    public class MediaFileService : IMediaFileService,
        IHandle<SeriesMovedEvent>,
        IHandleAsync<IssueDeletedEvent>,
        IHandleAsync<ModelEvent<RootFolder>>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly Logger _logger;

        public MediaFileService(IMediaFileRepository mediaFileRepository, IEventAggregator eventAggregator, Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ComicFile Add(ComicFile comicFile)
        {
            var addedFile = _mediaFileRepository.Insert(comicFile);
            _eventAggregator.PublishEvent(new ComicFileAddedEvent(addedFile));
            return addedFile;
        }

        public void AddMany(List<ComicFile> comicFiles)
        {
            _mediaFileRepository.InsertMany(comicFiles);
            foreach (var addedFile in comicFiles)
            {
                _eventAggregator.PublishEvent(new ComicFileAddedEvent(addedFile));
            }
        }

        public void Update(ComicFile comicFile)
        {
            _mediaFileRepository.Update(comicFile);
        }

        public void Update(List<ComicFile> comicFiles)
        {
            _mediaFileRepository.UpdateMany(comicFiles);
        }

        public void Delete(ComicFile comicFile, DeleteMediaFileReason reason)
        {
            _mediaFileRepository.Delete(comicFile);

            // If the comic file wasn't mapped to a track, don't publish an event
            if (comicFile.IssueId > 0)
            {
                _eventAggregator.PublishEvent(new ComicFileDeletedEvent(comicFile, reason));
            }
        }

        public void DeleteMany(List<ComicFile> comicFiles, DeleteMediaFileReason reason)
        {
            _mediaFileRepository.DeleteMany(comicFiles);

            // publish events where comic file was mapped to a track
            foreach (var comicFile in comicFiles.Where(x => x.IssueId > 0))
            {
                _eventAggregator.PublishEvent(new ComicFileDeletedEvent(comicFile, reason));
            }
        }

        public List<IFileInfo> FilterUnchangedFiles(List<IFileInfo> files, FilterFilesType filter)
        {
            if (filter == FilterFilesType.None)
            {
                return files;
            }

            _logger.Debug($"Filtering {files.Count} files for unchanged files");

            var knownFiles = GetFileWithPath(files.Select(x => x.FullName).ToList());
            _logger.Trace($"Got {knownFiles.Count} existing files");

            if (!knownFiles.Any())
            {
                return files;
            }

            var combined = files
                .Join(knownFiles,
                      f => f.FullName,
                      af => af.Path,
                      (f, af) => new { DiskFile = f, DbFile = af },
                      PathEqualityComparer.Instance)
                .ToList();
            _logger.Trace($"Matched paths for {combined.Count} files");

            List<IFileInfo> unwanted = null;
            if (filter == FilterFilesType.Known)
            {
                unwanted = combined
                    .Where(x => x.DiskFile.Length == x.DbFile.Size &&
                           Math.Abs((x.DiskFile.LastWriteTimeUtc - x.DbFile.Modified.ToUniversalTime()).TotalSeconds) <= 1)
                    .Select(x => x.DiskFile)
                    .ToList();
                _logger.Trace($"{unwanted.Count} unchanged existing files");
            }
            else if (filter == FilterFilesType.Matched)
            {
                unwanted = combined
                    .Where(x => x.DiskFile.Length == x.DbFile.Size &&
                           Math.Abs((x.DiskFile.LastWriteTimeUtc - x.DbFile.Modified.ToUniversalTime()).TotalSeconds) <= 1 &&
                           (x.DbFile.Issue == null || (x.DbFile.Issue.IsLoaded && x.DbFile.Issue.Value != null)))
                    .Select(x => x.DiskFile)
                    .ToList();
                _logger.Trace($"{unwanted.Count} unchanged and matched files");
            }
            else
            {
                throw new ArgumentException("Unrecognised value of FilterFilesType filter");
            }

            return files.Except(unwanted).ToList();
        }

        public ComicFile Get(int id)
        {
            return _mediaFileRepository.Get(id);
        }

        public List<ComicFile> Get(IEnumerable<int> ids)
        {
            return _mediaFileRepository.Get(ids).ToList();
        }

        public List<ComicFile> GetFilesWithBasePath(string path)
        {
            return _mediaFileRepository.GetFilesWithBasePath(path);
        }

        public List<ComicFile> GetFileWithPath(List<string> path)
        {
            return _mediaFileRepository.GetFileWithPath(path);
        }

        public ComicFile GetFileWithPath(string path)
        {
            return _mediaFileRepository.GetFileWithPath(path);
        }

        public List<ComicFile> GetFilesBySeries(int seriesId)
        {
            return _mediaFileRepository.GetFilesBySeries(seriesId);
        }

        public List<ComicFile> GetFilesBySeriesMetadataId(int seriesMetadataId)
        {
            return _mediaFileRepository.GetFilesBySeriesMetadataId(seriesMetadataId);
        }

        public List<ComicFile> GetFilesByIssue(int issueId)
        {
            return _mediaFileRepository.GetFilesByIssue(issueId);
        }

        public List<ComicFile> GetUnmappedFiles()
        {
            return _mediaFileRepository.GetUnmappedFiles();
        }

        public void UpdateMediaInfo(List<ComicFile> comicFiles)
        {
            _mediaFileRepository.SetFields(comicFiles, t => t.MediaInfo);
        }

        public void Handle(SeriesMovedEvent message)
        {
            var files = _mediaFileRepository.GetFilesWithBasePath(message.SourcePath);

            foreach (var file in files)
            {
                var newPath = message.DestinationPath + file.Path.Substring(message.SourcePath.Length);
                file.Path = newPath;
            }

            Update(files);
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                _mediaFileRepository.DeleteFilesByIssue(message.Issue.Id);
            }
            else
            {
                _mediaFileRepository.UnlinkFilesByIssue(message.Issue.Id);
            }
        }

        public void HandleAsync(ModelEvent<RootFolder> message)
        {
            if (message.Action == ModelAction.Deleted)
            {
                var files = GetFilesWithBasePath(message.Model.Path);
                DeleteMany(files, DeleteMediaFileReason.Manual);
            }
        }
    }
}
