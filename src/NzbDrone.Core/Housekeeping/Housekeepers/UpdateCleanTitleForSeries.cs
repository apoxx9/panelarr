using System.Linq;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class UpdateCleanTitleForSeries : IHousekeepingTask
    {
        private readonly ISeriesRepository _seriesRepository;

        public UpdateCleanTitleForSeries(ISeriesRepository seriesRepository)
        {
            _seriesRepository = seriesRepository;
        }

        public void Clean()
        {
            var allSeries = _seriesRepository.All().ToList();

            allSeries.ForEach(s =>
            {
                var cleanName = s.Name.CleanSeriesName();
                if (s.CleanName != cleanName)
                {
                    s.CleanName = cleanName;
                    _seriesRepository.Update(s);
                }
            });
        }
    }
}
