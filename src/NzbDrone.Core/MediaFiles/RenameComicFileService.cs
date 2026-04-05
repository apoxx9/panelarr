using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameComicFileService
    {
        List<RenameComicFilePreview> GetRenamePreviews(int seriesId);
        List<RenameComicFilePreview> GetRenamePreviews(int seriesId, int issueId);
    }

    public class RenameComicFileService : IRenameComicFileService, IExecute<RenameFilesCommand>, IExecute<RenameSeriesCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IIssueService _issueService;
        private readonly IMoveComicFiles _comicFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RenameComicFileService(ISeriesService seriesService,
                                        IMediaFileService mediaFileService,
                                        IIssueService issueService,
                                        IMoveComicFiles comicFileMover,
                                        IEventAggregator eventAggregator,
                                        IBuildFileNames filenameBuilder,
                                        IDiskProvider diskProvider,
                                        Logger logger)
        {
            _seriesService = seriesService;
            _mediaFileService = mediaFileService;
            _issueService = issueService;
            _comicFileMover = comicFileMover;
            _eventAggregator = eventAggregator;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameComicFilePreview> GetRenamePreviews(int seriesId)
        {
            var series = _seriesService.GetSeries(seriesId);
            var files = _mediaFileService.GetFilesBySeries(seriesId);

            _logger.Trace($"got {files.Count} files");

            return GetPreviews(series, files)
                .OrderByDescending(e => e.IssueId)
                .ThenBy(e => e.ExistingPath)
                .ToList();
        }

        public List<RenameComicFilePreview> GetRenamePreviews(int seriesId, int issueId)
        {
            var series = _seriesService.GetSeries(seriesId);
            var files = _mediaFileService.GetFilesByIssue(issueId);

            return GetPreviews(series, files)
                .OrderBy(e => e.ExistingPath).ToList();
        }

        private IEnumerable<RenameComicFilePreview> GetPreviews(Series series, List<ComicFile> files)
        {
            var counts = files.GroupBy(x => x.IssueId).ToDictionary(g => g.Key, g => g.Count());

            foreach (var f in files)
            {
                var file = f;
                file.PartCount = counts[file.IssueId];

                var issue = file.Issue.Value;
                var comicFilePath = file.Path;

                if (issue == null)
                {
                    _logger.Warn("File ({0}) is not linked to a issue", comicFilePath);
                    continue;
                }

                var newName = _filenameBuilder.BuildComicFileName(series, issue, file);

                _logger.Trace($"got name {newName}");

                var newPath = _filenameBuilder.BuildComicFilePath(series, issue, newName, Path.GetExtension(comicFilePath));

                _logger.Trace($"got path {newPath}");

                if (!comicFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameComicFilePreview
                    {
                        SeriesId = series.Id,
                        IssueId = issue.Id,
                        ComicFileId = file.Id,
                        ExistingPath = file.Path,
                        NewPath = newPath
                    };
                }
            }
        }

        private void RenameFiles(List<ComicFile> comicFiles, Series series)
        {
            var allFiles = _mediaFileService.GetFilesBySeries(series.Id);
            var counts = allFiles.GroupBy(x => x.IssueId).ToDictionary(g => g.Key, g => g.Count());
            var renamed = new List<RenamedComicFile>();

            foreach (var comicFile in comicFiles)
            {
                var previousPath = comicFile.Path;
                comicFile.PartCount = counts[comicFile.IssueId];

                try
                {
                    _logger.Debug("Renaming issue file: {0}", comicFile);
                    _comicFileMover.MoveComicFile(comicFile, series);

                    _mediaFileService.Update(comicFile);

                    renamed.Add(new RenamedComicFile
                    {
                        ComicFile = comicFile,
                        PreviousPath = previousPath
                    });

                    _logger.Debug("Renamed issue file: {0}", comicFile);

                    _eventAggregator.PublishEvent(new ComicFileRenamedEvent(series, comicFile, previousPath));
                }
                catch (FileAlreadyExistsException ex)
                {
                    _logger.Warn("File not renamed, there is already a file at the destination: {0}", ex.Filename);
                }
                catch (SameFilenameException ex)
                {
                    _logger.Debug("File not renamed, source and destination are the same: {0}", ex.Filename);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to rename file {0}", previousPath);
                }
            }

            if (renamed.Any())
            {
                _eventAggregator.PublishEvent(new SeriesRenamedEvent(series, renamed));

                _logger.Debug("Removing Empty Subfolders from: {0}", series.Path);
                _diskProvider.RemoveEmptySubfolders(series.Path);
            }
        }

        public void Execute(RenameFilesCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var comicFiles = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Renaming {0} files for {1}", comicFiles.Count, series.Name);
            RenameFiles(comicFiles, series);
            _logger.ProgressInfo("Selected issue files renamed for {0}", series.Name);
        }

        public void Execute(RenameSeriesCommand message)
        {
            _logger.Debug("Renaming all files for selected series");
            var seriesToRename = _seriesService.GetSeries(message.SeriesIds);

            foreach (var series in seriesToRename)
            {
                var comicFiles = _mediaFileService.GetFilesBySeries(series.Id);
                _logger.ProgressInfo("Renaming all files in series: {0}", series.Name);
                RenameFiles(comicFiles, series);
                _logger.ProgressInfo("All issue files renamed for {0}", series.Name);
            }
        }
    }
}
