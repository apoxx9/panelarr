using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class DeletedComicFileSpecification : IDecisionEngineSpecification
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly IMediaFileService _issueService;
        private readonly Logger _logger;

        public DeletedComicFileSpecification(IDiskProvider diskProvider,
                                             IConfigService configService,
                                             IMediaFileService issueService,
                                             Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _issueService = issueService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Disk;
        public RejectionType Type => RejectionType.Temporary;

        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            if (!_configService.AutoUnmonitorPreviouslyDownloadedIssues)
            {
                return Decision.Accept();
            }

            if (searchCriteria != null)
            {
                _logger.Debug("Skipping deleted bookfile check during search");
                return Decision.Accept();
            }

            var missingTrackFiles = subject.Issues
                                             .SelectMany(v => _issueService.GetFilesByIssue(v.Id))
                                             .DistinctBy(v => v.Id)
                                             .Where(v => IsTrackFileMissing(subject.Series, v))
                                             .ToArray();

            if (missingTrackFiles.Any())
            {
                foreach (var missingTrackFile in missingTrackFiles)
                {
                    _logger.Trace("Issue file {0} is missing from disk.", missingTrackFile.Path);
                }

                _logger.Debug("Files for this issue exist in the database but not on disk, will be unmonitored on next diskscan. skipping.");
                return Decision.Reject("Series is not monitored");
            }

            return Decision.Accept();
        }

        private bool IsTrackFileMissing(Series series, ComicFile comicFile)
        {
            return !_diskProvider.FileExists(comicFile.Path);
        }
    }
}
