using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Extras;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.IssueImport
{
    public interface IImportApprovedIssues
    {
        List<ImportResult> Import(List<ImportDecision<LocalIssue>> decisions, bool replaceExisting, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto);
    }

    public class ImportApprovedIssues : IImportApprovedIssues
    {
        private static readonly RegexReplace PadNumbers = new RegexReplace(@"\d+", n => n.Value.PadLeft(9, '0'), RegexOptions.Compiled);

        private readonly IUpgradeMediaFiles _comicFileUpgrader;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IComicFormatConverter _comicFormatConverter;
        private readonly ISeriesService _seriesService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IIssueService _issueService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IExtraService _extraService;
        private readonly IDiskProvider _diskProvider;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportApprovedIssues(IUpgradeMediaFiles comicFileUpgrader,
                                   IMediaFileService mediaFileService,
                                   IMetadataTagService metadataTagService,
                                   IComicFormatConverter comicFormatConverter,
                                   ISeriesService seriesService,
                                   IAddSeriesService addSeriesService,
                                   IIssueService issueService,
                                   IRootFolderService rootFolderService,
                                   IRecycleBinProvider recycleBinProvider,
                                   IExtraService extraService,
                                   IDiskProvider diskProvider,
                                   IHistoryService historyService,
                                   IEventAggregator eventAggregator,
                                   IManageCommandQueue commandQueueManager,
                                   Logger logger)
        {
            _comicFileUpgrader = comicFileUpgrader;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _comicFormatConverter = comicFormatConverter;
            _seriesService = seriesService;
            _addSeriesService = addSeriesService;
            _issueService = issueService;
            _rootFolderService = rootFolderService;
            _recycleBinProvider = recycleBinProvider;
            _extraService = extraService;
            _diskProvider = diskProvider;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public List<ImportResult> Import(List<ImportDecision<LocalIssue>> decisions, bool replaceExisting, DownloadClientItem downloadClientItem = null, ImportMode importMode = ImportMode.Auto)
        {
            var importResults = new List<ImportResult>();
            var allImportedComicFiles = new List<ComicFile>();
            var allOldComicFiles = new List<ComicFile>();
            var addedSeries = new List<Series>();
            var addedIssues = new List<Issue>();

            var issueDecisions = decisions.Where(e => e.Item.Issue != null && e.Approved)
                .GroupBy(e => e.Item.Issue.ForeignIssueId).ToList();

            var iDecision = 1;
            foreach (var issueDecision in issueDecisions)
            {
                _logger.ProgressInfo("Importing issue {0}/{1} {2}", iDecision++, issueDecisions.Count, issueDecision.First().Item.Issue);

                var decisionList = issueDecision.ToList();

                var series = EnsureSeriesAdded(decisionList, addedSeries);

                if (series == null)
                {
                    // failed to add the series, carry on with next issue
                    continue;
                }

                var issue = EnsureIssueAdded(decisionList, addedIssues);

                if (issue == null)
                {
                    // failed to add the issue, carry on with next one
                    continue;
                }

                // Publish issue edited event.
                // Deliberately don't put in the old issue since we don't want to trigger an SeriesScan.
                _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));
            }

            var qualifiedImports = decisions.Where(c => c.Approved)
                .GroupBy(c => c.Item.Series.Id, (i, s) => s
                         .OrderByDescending(c => c.Item.Quality, new QualityModelComparer(s.First().Item.Series.QualityProfile))
                         .ThenByDescending(c => c.Item.Size))
                .SelectMany(c => c)
                .ToList();

            _logger.ProgressInfo("Importing {0} files", qualifiedImports.Count);
            _logger.Debug("Importing {0} files. Replace existing: {1}", qualifiedImports.Count, replaceExisting);

            var filesToAdd = new List<ComicFile>(qualifiedImports.Count);
            var trackImportedEvents = new List<TrackImportedEvent>(qualifiedImports.Count);

            foreach (var importDecision in qualifiedImports)
            {
                var localTrack = importDecision.Item;
                var oldFiles = new List<ComicFile>();

                try
                {
                    //check if already imported
                    if (importResults.Where(r => r.ImportDecision.Item.Issue.Id == localTrack.Issue.Id).Any(r => r.ImportDecision.Item.Part == localTrack.Part))
                    {
                        importResults.Add(new ImportResult(importDecision, "Issue has already been imported"));
                        continue;
                    }

                    localTrack.Issue.Series = localTrack.Series;

                    var comicFile = new ComicFile
                    {
                        Path = localTrack.Path.CleanFilePath(),
                        Part = localTrack.Part,
                        PartCount = localTrack.PartCount,
                        Size = localTrack.Size,
                        Modified = localTrack.Modified,
                        DateAdded = DateTime.UtcNow,
                        ReleaseGroup = localTrack.ReleaseGroup,
                        Quality = localTrack.Quality,
                        MediaInfo = localTrack.FileTrackInfo.MediaInfo,
                        IssueId = localTrack.Issue.Id,
                        Series = localTrack.Series,
                        Issue = localTrack.Issue,
                        ComicFormat = GetComicFormatFromPath(localTrack.Path)
                    };

                    if (downloadClientItem?.DownloadId.IsNotNullOrWhiteSpace() == true)
                    {
                        var grabHistory = _historyService.FindByDownloadId(downloadClientItem.DownloadId)
                            .OrderByDescending(h => h.Date)
                            .FirstOrDefault(h => h.EventType == EntityHistoryEventType.Grabbed);

                        if (Enum.TryParse(grabHistory?.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                        {
                            comicFile.IndexerFlags = flags;
                        }
                    }
                    else
                    {
                        comicFile.IndexerFlags = localTrack.IndexerFlags;
                    }

                    bool copyOnly;
                    switch (importMode)
                    {
                        default:
                        case ImportMode.Auto:
                            copyOnly = downloadClientItem != null && !downloadClientItem.CanMoveFiles;
                            break;
                        case ImportMode.Move:
                            copyOnly = false;
                            break;
                        case ImportMode.Copy:
                            copyOnly = true;
                            break;
                    }

                    if (!localTrack.ExistingFile)
                    {
                        // Convert mislabeled archives (RAR-as-CBZ) to real CBZ before import
                        _comicFormatConverter.NormalizeToCbz(localTrack.Path);

                        comicFile.SceneName = GetSceneReleaseName(downloadClientItem);

                        var moveResult = _comicFileUpgrader.UpgradeComicFile(comicFile, localTrack, copyOnly);
                        oldFiles = moveResult.OldFiles;
                    }
                    else
                    {
                        // Delete existing files from the DB mapped to this path
                        var previousFile = _mediaFileService.GetFileWithPath(comicFile.Path);

                        if (previousFile != null)
                        {
                            _mediaFileService.Delete(previousFile, DeleteMediaFileReason.ManualOverride);
                        }

                        _metadataTagService.WriteTags(comicFile, false);
                    }

                    filesToAdd.Add(comicFile);
                    importResults.Add(new ImportResult(importDecision));

                    if (!localTrack.ExistingFile)
                    {
                        _extraService.ImportTrack(localTrack, comicFile, copyOnly);
                    }

                    allImportedComicFiles.Add(comicFile);
                    allOldComicFiles.AddRange(oldFiles);

                    // create all the import events here, but we can't publish until the comic files have been
                    // inserted and ids created
                    trackImportedEvents.Add(new TrackImportedEvent(localTrack, comicFile, oldFiles, !localTrack.ExistingFile, downloadClientItem));
                }
                catch (RootFolderNotFoundException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, root folder missing."));
                }
                catch (DestinationAlreadyExistsException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, destination already exists."));
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, permissions error"));
                }
                catch (RecycleBinException e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    _eventAggregator.PublishEvent(new TrackImportFailedEvent(e, localTrack, !localTrack.ExistingFile, downloadClientItem));

                    importResults.Add(new ImportResult(importDecision, "Failed to import issue, unable to move existing file to the Recycle Bin."));
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Couldn't import issue " + localTrack);
                    importResults.Add(new ImportResult(importDecision, "Failed to import issue."));
                }
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            _mediaFileService.AddMany(filesToAdd);
            _logger.Debug("Inserted new comic files in {0}ms", watch.ElapsedMilliseconds);

            // now that comic files have been inserted and ids generated, publish the import events
            foreach (var trackImportedEvent in trackImportedEvents)
            {
                _eventAggregator.PublishEvent(trackImportedEvent);
            }

            var issueImports = importResults.Where(e => e.ImportDecision.Item.Issue != null)
                .GroupBy(e => e.ImportDecision.Item.Issue.Id).ToList();

            foreach (var issueImport in issueImports)
            {
                var issue = issueImport.First().ImportDecision.Item.Issue;
                var series = issueImport.First().ImportDecision.Item.Series;

                if (issueImport.Where(e => e.Errors.Count == 0).ToList().Count > 0 && series != null && issue != null)
                {
                    _eventAggregator.PublishEvent(new IssueImportedEvent(
                        series,
                        issue,
                        allImportedComicFiles.Where(s => s.IssueId == issue.Id).ToList(),
                        allOldComicFiles.Where(s => s.IssueId == issue.Id).ToList(),
                        replaceExisting,
                        downloadClientItem));
                }
            }

            //Adding all the rejected decisions
            importResults.AddRange(decisions.Where(c => !c.Approved)
                                            .Select(d => new ImportResult(d, d.Rejections.Select(r => r.Reason).ToArray())));

            // Refresh any allSeries we added
            if (addedSeries.Any())
            {
                _commandQueueManager.Push(new BulkRefreshSeriesCommand(addedSeries.Select(x => x.Id).ToList(), true));
            }

            var addedSeriesMetadataIds = addedSeries.Select(x => x.SeriesMetadataId).ToHashSet();
            var issuesToRefresh = addedIssues.Where(x => !addedSeriesMetadataIds.Contains(x.SeriesMetadataId)).ToList();

            if (issuesToRefresh.Any())
            {
                _logger.Debug("Refreshing info for {0} new issues", issuesToRefresh.Count);
                _commandQueueManager.Push(new BulkRefreshIssueCommand(issuesToRefresh.Select(x => x.Id).ToList()));
            }

            return importResults;
        }

        private Series EnsureSeriesAdded(List<ImportDecision<LocalIssue>> decisions, List<Series> addedSeries)
        {
            var series = decisions.First().Item.Series;

            if (series.Id == 0)
            {
                var dbSeries = _seriesService.FindById(series.ForeignSeriesId);

                if (dbSeries == null)
                {
                    _logger.Debug("Adding remote series {0}", series);

                    var path = decisions.First().Item.Path;
                    var rootFolder = _rootFolderService.GetBestRootFolder(path);

                    series.RootFolderPath = rootFolder.Path;
                    series.QualityProfileId = rootFolder.DefaultQualityProfileId;
                    series.Monitored = rootFolder.DefaultMonitorOption != MonitorTypes.None;
                    series.MonitorNewItems = rootFolder.DefaultNewItemMonitorOption;
                    series.Tags = rootFolder.DefaultTags;
                    series.AddOptions = new AddSeriesOptions
                    {
                        SearchForMissingIssues = false,
                        Monitored = series.Monitored,
                        Monitor = rootFolder.DefaultMonitorOption
                    };

                    try
                    {
                        dbSeries = _addSeriesService.AddSeries(series, false);

                        // this looks redundant but is necessary to get the LazyLoads populated
                        dbSeries = _seriesService.GetSeries(dbSeries.Id);
                        addedSeries.Add(dbSeries);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to add series {0}", series);
                        foreach (var decision in decisions)
                        {
                            decision.Reject(new Rejection("Failed to add missing series", RejectionType.Temporary));
                        }

                        return null;
                    }
                }

                // Put in the newly loaded series
                foreach (var decision in decisions)
                {
                    decision.Item.Series = dbSeries;
                    decision.Item.Issue.Series = dbSeries;
                    decision.Item.Issue.SeriesMetadataId = dbSeries.SeriesMetadataId;
                }

                series = dbSeries;
            }

            return series;
        }

        private Issue EnsureIssueAdded(List<ImportDecision<LocalIssue>> decisions, List<Issue> addedIssues)
        {
            var issue = decisions.First().Item.Issue;

            if (issue.Id == 0)
            {
                var dbIssue = _issueService.FindById(issue.ForeignIssueId);

                if (dbIssue == null)
                {
                    _logger.Debug("Adding remote issue {0}", issue);

                    if (issue.SeriesMetadataId == 0)
                    {
                        throw new InvalidOperationException("Cannot insert issue with SeriesMetadataId = 0");
                    }

                    try
                    {
                        issue.Monitored = issue.Series.Value.Monitored;
                        issue.Added = DateTime.UtcNow;
                        _issueService.InsertMany(new List<Issue> { issue });
                        addedIssues.Add(issue);

                        dbIssue = _issueService.FindById(issue.ForeignIssueId);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to add issue {0}", issue);
                        RejectIssue(decisions);

                        return null;
                    }
                }

                // Populate the new DB issue
                foreach (var decision in decisions)
                {
                    decision.Item.Issue = dbIssue;
                }

                issue = dbIssue;
            }

            return issue;
        }

        private void RejectIssue(List<ImportDecision<LocalIssue>> decisions)
        {
            foreach (var decision in decisions)
            {
                decision.Reject(new Rejection("Failed to add missing issue", RejectionType.Temporary));
            }
        }

        private void RemoveExistingComicFiles(Series series, Issue issue)
        {
            var rootFolder = _diskProvider.GetParentFolder(series.Path);
            var previousFiles = _mediaFileService.GetFilesByIssue(issue.Id);

            _logger.Debug("Deleting {0} existing files for {1}", previousFiles.Count, issue);

            foreach (var previousFile in previousFiles)
            {
                var subfolder = rootFolder.GetRelativePath(_diskProvider.GetParentFolder(previousFile.Path));
                if (_diskProvider.FileExists(previousFile.Path))
                {
                    _logger.Debug("Removing existing issue file: {0}", previousFile);
                    _recycleBinProvider.DeleteFile(previousFile.Path, subfolder);
                }

                _mediaFileService.Delete(previousFile, DeleteMediaFileReason.Upgrade);
            }
        }

        private string GetSceneReleaseName(DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem != null)
            {
                var title = Parser.Parser.RemoveFileExtension(downloadClientItem.Title);

                var parsedTitle = Parser.Parser.ParseIssueTitle(title);

                if (parsedTitle != null)
                {
                    return title;
                }
            }

            return null;
        }

        private static ComicFormat GetComicFormatFromPath(string path)
        {
            var ext = System.IO.Path.GetExtension(path)?.TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "cbz" => ComicFormat.CBZ,
                "cbr" => ComicFormat.CBR,
                "cb7" => ComicFormat.CB7,
                "pdf" => ComicFormat.PDF,
                _ => ComicFormat.Unknown
            };
        }
    }
}
