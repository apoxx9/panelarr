namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class SeriesSearchCriteria : SearchCriteriaBase
    {
        public override string ToString()
        {
            return $"[{Series.Name}]";
        }
    }
}
