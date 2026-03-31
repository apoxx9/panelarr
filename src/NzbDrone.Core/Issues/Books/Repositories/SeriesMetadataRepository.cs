using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesMetadataRepository : IBasicRepository<SeriesMetadata>
    {
        List<SeriesMetadata> FindById(List<string> foreignIds);
        bool UpsertMany(List<SeriesMetadata> data);
    }

    public class SeriesMetadataRepository : BasicRepository<SeriesMetadata>, ISeriesMetadataRepository
    {
        private readonly Logger _logger;

        public SeriesMetadataRepository(IMainDatabase database, IEventAggregator eventAggregator, Logger logger)
            : base(database, eventAggregator)
        {
            _logger = logger;
        }

        public List<SeriesMetadata> FindById(List<string> foreignIds)
        {
            return Query(x => Enumerable.Contains(foreignIds, x.ForeignSeriesId));
        }

        public bool UpsertMany(List<SeriesMetadata> data)
        {
            var existingMetadata = FindById(data.Select(x => x.ForeignSeriesId).ToList());
            var updateMetadataList = new List<SeriesMetadata>();
            var addMetadataList = new List<SeriesMetadata>();
            var upToDateMetadataCount = 0;

            foreach (var meta in data)
            {
                var existing = existingMetadata.SingleOrDefault(x => x.ForeignSeriesId == meta.ForeignSeriesId);
                if (existing != null)
                {
                    // populate Id in remote data
                    meta.UseDbFieldsFrom(existing);

                    // responses vary, so try adding remote to what we have
                    if (!meta.Equals(existing))
                    {
                        updateMetadataList.Add(meta);
                    }
                    else
                    {
                        upToDateMetadataCount++;
                    }
                }
                else
                {
                    addMetadataList.Add(meta);
                }
            }

            UpdateMany(updateMetadataList);
            InsertMany(addMetadataList);

            _logger.Debug($"{upToDateMetadataCount} series metadata up to date; Updating {updateMetadataList.Count}, Adding {addMetadataList.Count} series metadata entries.");

            return updateMetadataList.Count > 0 || addMetadataList.Count > 0;
        }
    }
}
