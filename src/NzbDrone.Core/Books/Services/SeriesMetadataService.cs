using System.Collections.Generic;

namespace NzbDrone.Core.Books
{
    public interface ISeriesMetadataService
    {
        bool Upsert(SeriesMetadata author);
        bool UpsertMany(List<SeriesMetadata> authors);
    }

    public class SeriesMetadataService : ISeriesMetadataService
    {
        private readonly ISeriesMetadataRepository _authorMetadataRepository;

        public SeriesMetadataService(ISeriesMetadataRepository authorMetadataRepository)
        {
            _authorMetadataRepository = authorMetadataRepository;
        }

        public bool Upsert(SeriesMetadata author)
        {
            return _authorMetadataRepository.UpsertMany(new List<SeriesMetadata> { author });
        }

        public bool UpsertMany(List<SeriesMetadata> authors)
        {
            return _authorMetadataRepository.UpsertMany(authors);
        }
    }
}
