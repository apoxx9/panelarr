using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public class CreditPopulationService :
        IHandle<ComicFileAddedEvent>,
        IHandle<SeriesScannedEvent>
    {
        private readonly ICreditExtractorService _creditExtractor;
        private readonly IIssueService _issueService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public CreditPopulationService(
            ICreditExtractorService creditExtractor,
            IIssueService issueService,
            IMediaFileService mediaFileService,
            Logger logger)
        {
            _creditExtractor = creditExtractor;
            _issueService = issueService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public void Handle(ComicFileAddedEvent message)
        {
            PopulateCredits(message.ComicFile);
        }

        public void Handle(SeriesScannedEvent message)
        {
            BackfillCredits(message.Series);
        }

        private void PopulateCredits(ComicFile comicFile)
        {
            var issue = comicFile.Issue?.Value ?? _issueService.GetIssue(comicFile.IssueId);
            if (issue == null)
            {
                _logger.Warn("Issue not found for ComicFile {0}, skipping credit extraction", comicFile.Id);
                return;
            }

            var credits = _creditExtractor.ExtractCredits(comicFile);

            if (credits.Count == 0)
            {
                return;
            }

            issue.Credits = credits;
            _issueService.UpdateIssue(issue);
            _logger.Debug("Extracted {0} credits from {1}", credits.Count, comicFile.Path);
        }

        private void BackfillCredits(Series series)
        {
            var files = _mediaFileService.GetFilesBySeries(series.Id);

            if (!files.Any())
            {
                return;
            }

            var issueIds = files.Where(f => f.IssueId > 0).Select(f => f.IssueId).Distinct().ToList();
            var issues = _issueService.GetIssues(issueIds);
            var issuesWithoutCredits = issues.Where(i => i.Credits == null || !i.Credits.Any()).ToList();

            if (!issuesWithoutCredits.Any())
            {
                return;
            }

            var filesByIssueId = files.Where(f => f.IssueId > 0).GroupBy(f => f.IssueId).ToDictionary(g => g.Key, g => g.First());
            var updated = new List<Issue>();

            foreach (var issue in issuesWithoutCredits)
            {
                if (!filesByIssueId.TryGetValue(issue.Id, out var comicFile))
                {
                    continue;
                }

                var credits = _creditExtractor.ExtractCredits(comicFile);

                if (credits.Count == 0)
                {
                    continue;
                }

                issue.Credits = credits;
                updated.Add(issue);
            }

            if (updated.Any())
            {
                _issueService.UpdateMany(updated);
                _logger.Debug("Backfilled credits for {0} issues in {1}", updated.Count, series.Name);
            }
        }
    }
}
