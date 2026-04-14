using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class SameFileSpecification : IImportDecisionEngineSpecification<LocalIssue>
    {
        private readonly Logger _logger;

        public SameFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalIssue localIssue, DownloadClientItem downloadClientItem)
        {
            var comicFiles = localIssue.Issue?.ComicFiles?.Value;

            if (comicFiles == null || !comicFiles.Any())
            {
                _logger.Debug("No existing issue file, skipping");
                return Decision.Accept();
            }

            foreach (var comicFile in comicFiles)
            {
                if (comicFile == null)
                {
                    var issue = localIssue.Issue;
                    _logger.Trace("Unable to get issue file details from the DB. IssueId: {0}", issue.Id);

                    return Decision.Accept();
                }

                if (comicFile.Size == localIssue.Size)
                {
                    _logger.Debug("'{0}' Has the same filesize as existing file", localIssue.Path);
                    return Decision.Reject("Has the same filesize as existing file");
                }
            }

            return Decision.Accept();
        }
    }
}
