using System.Collections.Generic;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesMetadataService
    {
        bool Upsert(SeriesMetadata series);
        bool UpsertMany(List<SeriesMetadata> allSeries);
    }

    public class SeriesMetadataService : ISeriesMetadataService
    {
        private readonly ISeriesMetadataRepository _seriesMetadataRepository;

        public SeriesMetadataService(ISeriesMetadataRepository seriesMetadataRepository)
        {
            _seriesMetadataRepository = seriesMetadataRepository;
        }

        public bool Upsert(SeriesMetadata series)
        {
            return _seriesMetadataRepository.UpsertMany(new List<SeriesMetadata> { series });
        }

        public bool UpsertMany(List<SeriesMetadata> allSeries)
        {
            return _seriesMetadataRepository.UpsertMany(allSeries);
        }
    }
}
