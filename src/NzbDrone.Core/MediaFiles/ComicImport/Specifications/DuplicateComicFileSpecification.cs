using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    /// <summary>
    /// Rejects a new file for import when the issue already has one or more ComicFiles
    /// and the new file does not represent a quality upgrade.
    ///
    /// Resolution logic:
    ///   - If the existing files list is empty → Accept (first import).
    ///   - If new file quality is strictly higher than ALL existing files → Accept
    ///     (the upgrade pipeline will remove the old file via UpgradeMediaFileService).
    ///   - Otherwise → Reject with "Existing file is equal or better quality".
    ///
    /// Note: ImageQualityScore is populated asynchronously after import, so it is not
    /// available at decision time.  Quality-format comparison (CBZ > CBR > PDF etc.) is
    /// sufficient here; DuplicateComicFileService handles post-import resolution using
    /// ImageQualityScore.
    /// </summary>
    public class DuplicateComicFileSpecification : IImportDecisionEngineSpecification<LocalIssue>
    {
        private readonly Logger _logger;

        public DuplicateComicFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalIssue item, DownloadClientItem downloadClientItem)
        {
            var existingFiles = item.Issue?.ComicFiles?.Value;
            if (existingFiles == null || !existingFiles.Any())
            {
                // No existing file — allow import.
                return Decision.Accept();
            }

            // Only applies to existing-file imports (local scans), not new downloads.
            // For new downloads the UpgradeSpecification already handles upgrade logic.
            if (!item.ExistingFile)
            {
                return Decision.Accept();
            }

            var qualityComparer = new QualityModelComparer(item.Series.QualityProfile);

            // If new file quality is higher than every existing file → it's an upgrade → accept.
            var allExistingAreLower = existingFiles.All(f =>
                qualityComparer.Compare(item.Quality, f.Quality) > 0);

            if (allExistingAreLower)
            {
                _logger.Debug(
                    "DuplicateComicFileSpecification: new file is a quality upgrade for {0}. Accepting.",
                    item.Path);
                return Decision.Accept();
            }

            _logger.Debug(
                "DuplicateComicFileSpecification: issue already has {0} file(s) of equal or better quality. Rejecting {1}.",
                existingFiles.Count,
                item.Path);

            return Decision.Reject("Existing file is equal or better quality");
        }
    }
}
