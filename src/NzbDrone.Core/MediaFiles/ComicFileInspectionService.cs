using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    // Archive inspection (page count + image quality score) and credit
    // extraction both require reading the archive itself. Running them
    // synchronously on the import event path ground bulk imports to a silent
    // multi-hour crawl over NFS, so the work lives here instead: a queued
    // command that discovers its work by query (mapped files that were never
    // inspected), opens each archive exactly once for both jobs, and reports
    // progress. Query-driven discovery makes it self-healing — anything
    // missed by a crash or restart is picked up on the next run.
    public class ComicFileInspectionService : IExecute<InspectComicFilesCommand>,
                                              IHandle<ComicFileAddedEvent>,
                                              IHandle<SeriesScannedEvent>
    {
        private const int UpdateBatchSize = 25;

        private readonly IMediaFileService _mediaFileService;
        private readonly IIssueService _issueService;
        private readonly IArchiveInspector _archiveInspector;
        private readonly ICreditExtractorService _creditExtractor;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ComicFileInspectionService(IMediaFileService mediaFileService,
                                          IIssueService issueService,
                                          IArchiveInspector archiveInspector,
                                          ICreditExtractorService creditExtractor,
                                          IManageCommandQueue commandQueueManager,
                                          Logger logger)
        {
            _mediaFileService = mediaFileService;
            _issueService = issueService;
            _archiveInspector = archiveInspector;
            _creditExtractor = creditExtractor;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public void Execute(InspectComicFilesCommand message)
        {
            // Failed inspections keep ImageCount == 0 and would re-appear in
            // the query forever — attempt each file at most once per run and
            // loop so files mapped while we were working are still covered.
            var attempted = new HashSet<int>();
            var processed = 0;

            while (true)
            {
                var pending = _mediaFileService.GetFilesPendingInspection()
                    .Where(f => f.Path.IsNotNullOrWhiteSpace() && !attempted.Contains(f.Id))
                    .ToList();

                if (pending.Empty())
                {
                    break;
                }

                var total = processed + pending.Count;

                var issues = _issueService.GetIssues(pending.Select(f => f.IssueId).Distinct().ToList())
                    .ToDictionary(i => i.Id);

                var filesToUpdate = new List<ComicFile>();
                var issuesToUpdate = new List<Issue>();

                foreach (var file in pending)
                {
                    attempted.Add(file.Id);
                    processed++;

                    _logger.ProgressInfo("Inspecting archives {0}/{1}", processed, total);

                    var result = _archiveInspector.Inspect(file.Path, file.ComicFormat);

                    if (result.Error != null)
                    {
                        _logger.Warn("Archive inspection returned error for {0}: {1}", file.Path, result.Error);
                    }

                    file.ImageCount = result.ImageCount > 0 ? result.ImageCount : result.PageCount;
                    file.ImageQualityScore = result.ImageQualityScore;
                    filesToUpdate.Add(file);

                    if (result.ComicInfoXml.IsNotNullOrWhiteSpace() &&
                        issues.TryGetValue(file.IssueId, out var issue) &&
                        (issue.Credits == null || !issue.Credits.Any()))
                    {
                        var credits = _creditExtractor.ParseCredits(result.ComicInfoXml);

                        if (credits.Any())
                        {
                            issue.Credits = credits;
                            issuesToUpdate.Add(issue);
                            _logger.Debug("Extracted {0} credits from {1}", credits.Count, file.Path);
                        }
                    }

                    if (filesToUpdate.Count >= UpdateBatchSize)
                    {
                        Flush(filesToUpdate, issuesToUpdate);
                    }
                }

                Flush(filesToUpdate, issuesToUpdate);
            }

            _logger.ProgressInfo("Archive inspection complete: {0} files inspected", processed);
        }

        public void Handle(ComicFileAddedEvent message)
        {
            // Only mapped files are inspected; bulk scans register thousands
            // of unmapped rows and must not queue work for them
            if (message.ComicFile.IssueId > 0)
            {
                _commandQueueManager.Push(new InspectComicFilesCommand());
            }
        }

        public void Handle(SeriesScannedEvent message)
        {
            // Covers files that became mapped without an add event (e.g. a
            // rescan matching previously unmapped rows)
            if (_mediaFileService.GetFilesPendingInspection().Any())
            {
                _commandQueueManager.Push(new InspectComicFilesCommand());
            }
        }

        private void Flush(List<ComicFile> files, List<Issue> issues)
        {
            if (files.Any())
            {
                _mediaFileService.Update(files.ToList());
                files.Clear();
            }

            if (issues.Any())
            {
                _issueService.UpdateMany(issues.ToList());
                issues.Clear();
            }
        }
    }
}
