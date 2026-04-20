using System.Linq;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Localization;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(SeriesDeletedEvent))]
    [CheckOn(typeof(SeriesMovedEvent))]
    [CheckOn(typeof(TrackImportedEvent), CheckOnCondition.FailedOnly)]
    [CheckOn(typeof(TrackImportFailedEvent), CheckOnCondition.SuccessfulOnly)]
    public class RootFolderCheck : HealthCheckBase
    {
        private readonly ISeriesService _seriesService;
        private readonly IImportListFactory _importListFactory;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;

        public RootFolderCheck(ISeriesService seriesService, IImportListFactory importListFactory, IDiskProvider diskProvider, IRootFolderService rootFolderService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _seriesService = seriesService;
            _importListFactory = importListFactory;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
        }

        public override HealthCheck Check()
        {
            var rootFolders = _seriesService.AllSeriesPaths()
                                                           .Select(s => _rootFolderService.GetBestRootFolderPath(s.Value))
                                                           .Distinct();

            var missingRootFolders = rootFolders.Where(s => !s.IsPathValid(PathValidationType.CurrentOs) || !_diskProvider.FolderExists(s))
                .ToList();

            missingRootFolders.AddRange(_importListFactory.All()
                .Select(s => s.RootFolderPath)
                .Distinct()
                .Where(s => !_diskProvider.FolderExists(s))
                .ToList());

            missingRootFolders = missingRootFolders.Distinct().ToList();

            if (missingRootFolders.Any())
            {
                if (missingRootFolders.Count == 1)
                {
                    return new HealthCheck(GetType(), HealthCheckResult.Error, string.Format(_localizationService.GetLocalizedString("RootFolderCheckSingleMessage"), missingRootFolders.First()), "#missing-root-folder");
                }

                var message = string.Format(_localizationService.GetLocalizedString("RootFolderCheckMultipleMessage"), string.Join(" | ", missingRootFolders));
                return new HealthCheck(GetType(), HealthCheckResult.Error, message, "#missing-root-folder");
            }

            return new HealthCheck(GetType());
        }
    }
}
