using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation
{
    public interface IAugmentingService
    {
        LocalIssue Augment(LocalIssue localTrack, bool otherFiles);
        LocalEdition Augment(LocalEdition localIssue);
    }

    public class AugmentingService : IAugmentingService
    {
        private readonly IEnumerable<IAggregate<LocalIssue>> _trackAugmenters;
        private readonly IEnumerable<IAggregate<LocalEdition>> _issueAugmenters;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public AugmentingService(IEnumerable<IAggregate<LocalIssue>> trackAugmenters,
                                 IEnumerable<IAggregate<LocalEdition>> issueAugmenters,
                                 IDiskProvider diskProvider,
                                 Logger logger)
        {
            _trackAugmenters = trackAugmenters;
            _issueAugmenters = issueAugmenters;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public LocalIssue Augment(LocalIssue localTrack, bool otherFiles)
        {
            if (localTrack.DownloadClientIssueInfo == null &&
                localTrack.FolderTrackInfo == null &&
                localTrack.FileTrackInfo == null)
            {
                if (MediaFileExtensions.AllExtensions.Contains(Path.GetExtension(localTrack.Path)))
                {
                    throw new AugmentingFailedException("Unable to parse issue info from path: {0}", localTrack.Path);
                }
            }

            localTrack.Size = _diskProvider.GetFileSize(localTrack.Path);
            localTrack.SceneName = localTrack.SceneSource ? SceneNameCalculator.GetSceneName(localTrack) : null;

            foreach (var augmenter in _trackAugmenters)
            {
                try
                {
                    augmenter.Aggregate(localTrack, otherFiles);
                }
                catch (Exception ex)
                {
                    var message = $"Unable to augment information for file: '{localTrack.Path}'. Series: {localTrack.Series} Error: {ex.Message}";

                    _logger.Warn(ex, ex.Message);
                }
            }

            return localTrack;
        }

        public LocalEdition Augment(LocalEdition localIssue)
        {
            foreach (var augmenter in _issueAugmenters)
            {
                try
                {
                    augmenter.Aggregate(localIssue, false);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, ex.Message);
                }
            }

            return localIssue;
        }
    }
}
