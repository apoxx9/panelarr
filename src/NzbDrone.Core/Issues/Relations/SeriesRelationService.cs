using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues.Relations
{
    public interface ISeriesRelationService
    {
        SeriesRelation Add(SeriesRelation relation);
        List<SeriesRelation> GetBySeriesId(int seriesId);
        SeriesRelation Get(int id);
        void Delete(int id);
    }

    public class SeriesRelationService : ISeriesRelationService, IHandleAsync<SeriesDeletedEvent>
    {
        private readonly ISeriesRelationRepository _repo;
        private readonly ISeriesService _seriesService;

        public SeriesRelationService(ISeriesRelationRepository repo, ISeriesService seriesService)
        {
            _repo = repo;
            _seriesService = seriesService;
        }

        public SeriesRelation Add(SeriesRelation relation)
        {
            if (relation.SeriesId == relation.RelatedSeriesId)
            {
                throw new ArgumentException("A series cannot be related to itself");
            }

            var found = _seriesService.GetSeries(new[] { relation.SeriesId, relation.RelatedSeriesId });

            if (found.Count != 2)
            {
                throw new ArgumentException("Both series must exist in the library");
            }

            // One link per pair: symmetric rendering makes a reverse link a
            // duplicate, whatever its type.
            var existing = _repo.FindBySeriesId(relation.SeriesId);

            if (existing.Any(r => r.SeriesId == relation.RelatedSeriesId || r.RelatedSeriesId == relation.RelatedSeriesId))
            {
                throw new ArgumentException("These series are already related");
            }

            return _repo.Insert(relation);
        }

        public List<SeriesRelation> GetBySeriesId(int seriesId)
        {
            return _repo.FindBySeriesId(seriesId);
        }

        public SeriesRelation Get(int id)
        {
            return _repo.Get(id);
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            var relations = _repo.FindBySeriesId(message.Series.Id);

            if (relations.Any())
            {
                _repo.DeleteMany(relations);
            }
        }
    }
}
