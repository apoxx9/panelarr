using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class SameFileSpecification : IImportDecisionEngineSpecification<LocalBook>
    {
        private readonly Logger _logger;

        public SameFileSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalBook localBook, DownloadClientItem downloadClientItem)
        {
            var comicFiles = localBook.Issue?.ComicFiles?.Value;

            if (comicFiles == null || !comicFiles.Any())
            {
                _logger.Debug("No existing issue file, skipping");
                return Decision.Accept();
            }

            foreach (var comicFile in comicFiles)
            {
                if (comicFile == null)
                {
                    var issue = localBook.Issue;
                    _logger.Trace("Unable to get issue file details from the DB. IssueId: {0}", issue.Id);

                    return Decision.Accept();
                }

                if (comicFile.Size == localBook.Size)
                {
                    _logger.Debug("'{0}' Has the same filesize as existing file", localBook.Path);
                    return Decision.Reject("Has the same filesize as existing file");
                }
            }

            return Decision.Accept();
        }
    }
}
