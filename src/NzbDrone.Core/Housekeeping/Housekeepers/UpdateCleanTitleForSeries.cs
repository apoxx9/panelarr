using System.Linq;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class UpdateCleanTitleForSeries : IHousekeepingTask
    {
        private readonly ISeriesRepository _authorRepository;

        public UpdateCleanTitleForSeries(ISeriesRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public void Clean()
        {
            var authors = _authorRepository.All().ToList();

            authors.ForEach(s =>
            {
                var cleanName = s.Name.CleanSeriesName();
                if (s.CleanName != cleanName)
                {
                    s.CleanName = cleanName;
                    _authorRepository.Update(s);
                }
            });
        }
    }
}
