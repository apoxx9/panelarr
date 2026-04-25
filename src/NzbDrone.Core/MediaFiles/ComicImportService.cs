using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IComicImportService
    {
        /// <summary>
        /// Scan a folder for comic files, run import decisions, and import approved files.
        /// Returns a summary of the import results.
        /// </summary>
        ComicImportResult ImportFolder(string folderPath, ImportMode importMode = ImportMode.Auto);
    }

    public class ComicImportResult
    {
        public string Folder { get; set; }
        public int TotalFiles { get; set; }
        public int Imported { get; set; }
        public int Rejected { get; set; }
        public List<string> UnmatchedPaths { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ComicImportService : IComicImportService, IExecute<ImportComicsCommand>
    {
        private readonly IDiskScanService _diskScanService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedIssues _importApprovedIssues;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public ComicImportService(IDiskScanService diskScanService,
                                   IMakeImportDecision importDecisionMaker,
                                   IImportApprovedIssues importApprovedIssues,
                                   IRootFolderService rootFolderService,
                                   IDiskProvider diskProvider,
                                   Logger logger)
        {
            _diskScanService = diskScanService;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedIssues = importApprovedIssues;
            _rootFolderService = rootFolderService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public ComicImportResult ImportFolder(string folderPath, ImportMode importMode = ImportMode.Auto)
        {
            var result = new ComicImportResult { Folder = folderPath };

            if (folderPath.IsNullOrWhiteSpace() || !_diskProvider.FolderExists(folderPath))
            {
                _logger.Warn("Comic import folder does not exist: {0}", folderPath);
                result.Errors.Add($"Folder does not exist: {folderPath}");
                return result;
            }

            _logger.ProgressInfo("Starting comic import scan for: {0}", folderPath);

            // 1. Discover all comic files in the folder
            var comicFiles = _diskScanService.GetComicFiles(folderPath).ToList();
            result.TotalFiles = comicFiles.Count;

            if (!comicFiles.Any())
            {
                _logger.Info("No comic files found in: {0}", folderPath);
                return result;
            }

            _logger.Debug("Found {0} comic files in {1}", comicFiles.Count, folderPath);

            // 2. Build import decision maker config — treat these as existing files if they're in a root folder
            var rootFolder = _rootFolderService.GetBestRootFolder(folderPath);
            var isExistingLibraryPath = rootFolder != null;

            var config = new ImportDecisionMakerConfig
            {
                Filter = isExistingLibraryPath ? FilterFilesType.Known : FilterFilesType.None,
                NewDownload = !isExistingLibraryPath,
                SingleRelease = false,
                IncludeExisting = true,
                AddNewSeries = false,
                KeepAllEditions = false
            };

            // 3. Run import decisions — the pipeline uses ComicParser internally via AggregateFilenameInfo
            var decisions = _importDecisionMaker.GetImportDecisions(comicFiles, null, null, config);

            // 4. Separate approved from rejected
            var approvedDecisions = decisions.Where(d => d.Approved).ToList();
            var rejectedDecisions = decisions.Where(d => !d.Approved).ToList();

            _logger.Debug("{0} approved, {1} rejected", approvedDecisions.Count, rejectedDecisions.Count);

            // 5. Track unmatched files (rejected with "Couldn't find" or "Couldn't parse" reason)
            foreach (var rejected in rejectedDecisions)
            {
                var isUnmatched = rejected.Rejections.Any(r =>
                    r.Reason.Contains("Couldn't find") ||
                    r.Reason.Contains("Couldn't parse") ||
                    r.Reason.Contains("similar issue"));

                if (isUnmatched)
                {
                    result.UnmatchedPaths.Add(rejected.Item.Path);
                    _logger.Debug("Unmatched comic file (queued for manual import): {0}", rejected.Item.Path);
                }
                else
                {
                    result.Errors.Add($"{rejected.Item.Path}: {string.Join(", ", rejected.Rejections.Select(r => r.Reason))}");
                }
            }

            // 6. Import approved files
            if (approvedDecisions.Any())
            {
                var importResults = _importApprovedIssues.Import(approvedDecisions, false, null, importMode);
                result.Imported = importResults.Count(r => r.Result == ImportResultType.Imported);

                var failedImports = importResults.Where(r => r.Result != ImportResultType.Imported).ToList();
                foreach (var failed in failedImports)
                {
                    result.Errors.AddRange(failed.Errors);
                }
            }

            result.Rejected = rejectedDecisions.Count;

            _logger.ProgressInfo("Comic import complete for {0}: {1} imported, {2} unmatched, {3} errors",
                folderPath,
                result.Imported,
                result.UnmatchedPaths.Count,
                result.Errors.Count);

            return result;
        }

        public void Execute(ImportComicsCommand message)
        {
            if (message.Path.IsNotNullOrWhiteSpace())
            {
                ImportFolder(message.Path, message.ImportMode);
            }
            else
            {
                // Scan all root folders
                var rootFolders = _rootFolderService.All();
                foreach (var rootFolder in rootFolders)
                {
                    ImportFolder(rootFolder.Path, message.ImportMode);
                }
            }
        }
    }
}
