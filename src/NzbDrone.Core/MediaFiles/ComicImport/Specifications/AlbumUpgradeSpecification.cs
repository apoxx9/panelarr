using System.Linq;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class IssueUpgradeSpecification : IImportDecisionEngineSpecification<LocalEdition>
    {
        private readonly Logger _logger;

        public IssueUpgradeSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEdition item, DownloadClientItem downloadClientItem)
        {
            var qualityComparer = new QualityModelComparer(item.Issue?.Series.Value.QualityProfile);

            // min quality of all new tracks
            var newMinQuality = item.LocalIssues.Select(x => x.Quality).OrderBy(x => x, qualityComparer).First();
            _logger.Debug("Min quality of new files: {0}", newMinQuality);

            // get minimum quality of existing release
            // var existingQualities = currentRelease.Value.Where(x => x.TrackFileId != 0).Select(x => x.TrackFile.Value.Quality);
            // if (existingQualities.Any())
            // {
            //     var existingMinQuality = existingQualities.OrderBy(x => x, qualityComparer).First();
            //     _logger.Debug("Min quality of existing files: {0}", existingMinQuality);
            //     if (qualityComparer.Compare(existingMinQuality, newMinQuality) > 0)
            //     {
            //         _logger.Debug("This issue isn't a quality upgrade for all tracks. Skipping {0}", item);
            //         return Decision.Reject("Not an upgrade for existing issue file(s)");
            //     }
            // }
            return Decision.Accept();
        }
    }
}
