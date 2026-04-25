using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesMetadataService
    {
        bool Upsert(SeriesMetadata series);
        bool UpsertMany(List<SeriesMetadata> allSeries);
    }

    public class SeriesMetadataService : ISeriesMetadataService, IHandle<SeriesDeletedEvent>
    {
        private readonly ISeriesMetadataRepository _seriesMetadataRepository;
        private readonly Logger _logger;

        public SeriesMetadataService(ISeriesMetadataRepository seriesMetadataRepository, Logger logger)
        {
            _seriesMetadataRepository = seriesMetadataRepository;
            _logger = logger;
        }

        public bool Upsert(SeriesMetadata series)
        {
            return _seriesMetadataRepository.UpsertMany(new List<SeriesMetadata> { series });
        }

        public bool UpsertMany(List<SeriesMetadata> allSeries)
        {
            return _seriesMetadataRepository.UpsertMany(allSeries);
        }

        public void Handle(SeriesDeletedEvent message)
        {
            var metadataId = message.Series.SeriesMetadataId;

            if (metadataId > 0)
            {
                _logger.Debug("Deleting orphaned SeriesMetadata {0} for deleted series {1}", metadataId, message.Series.Name);
                _seriesMetadataRepository.Delete(metadataId);
            }
        }
    }
}
