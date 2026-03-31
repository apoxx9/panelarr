using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesGroupService
    {
        SeriesGroup GetSeriesGroup(int id);
        List<SeriesGroup> GetAllSeriesGroups();
        SeriesGroup FindById(string foreignSeriesId);
        List<SeriesGroup> FindById(List<string> foreignSeriesId);
        List<SeriesGroup> GetBySeriesMetadataId(int seriesMetadataId);
        List<SeriesGroup> GetBySeriesId(int seriesId);
        SeriesGroup AddSeriesGroup(SeriesGroup seriesGroup);
        SeriesGroup UpdateSeriesGroup(SeriesGroup seriesGroup);
        void Delete(int seriesId);
        void InsertMany(IList<SeriesGroup> series);
        void UpdateMany(IList<SeriesGroup> series);
    }

    public class SeriesGroupService : ISeriesGroupService
    {
        private readonly ISeriesGroupRepository _seriesRepository;

        public SeriesGroupService(ISeriesGroupRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public SeriesGroup GetSeriesGroup(int id)
        {
            return _seriesRepository.Get(id);
        }

        public List<SeriesGroup> GetAllSeriesGroups()
        {
            return _seriesRepository.All().ToList();
        }

        public SeriesGroup FindById(string foreignSeriesId)
        {
            return _seriesRepository.FindById(foreignSeriesId);
        }

        public List<SeriesGroup> FindById(List<string> foreignSeriesId)
        {
            return _seriesRepository.FindById(foreignSeriesId);
        }

        public List<SeriesGroup> GetBySeriesMetadataId(int seriesMetadataId)
        {
            return _seriesRepository.GetBySeriesMetadataId(seriesMetadataId);
        }

        public List<SeriesGroup> GetBySeriesId(int seriesId)
        {
            return _seriesRepository.GetBySeriesId(seriesId);
        }

        public SeriesGroup AddSeriesGroup(SeriesGroup seriesGroup)
        {
            return _seriesRepository.Insert(seriesGroup);
        }

        public SeriesGroup UpdateSeriesGroup(SeriesGroup seriesGroup)
        {
            return _seriesRepository.Update(seriesGroup);
        }

        public void Delete(int seriesId)
        {
            _seriesRepository.Delete(seriesId);
        }

        public void InsertMany(IList<SeriesGroup> series)
        {
            _seriesRepository.InsertMany(series);
        }

        public void UpdateMany(IList<SeriesGroup> series)
        {
            _seriesRepository.UpdateMany(series);
        }
    }
}
