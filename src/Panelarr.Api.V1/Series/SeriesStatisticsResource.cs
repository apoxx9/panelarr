using NzbDrone.Core.SeriesStats;

namespace Panelarr.Api.V1.Series
{
    public class SeriesStatisticsResource
    {
        public int IssueFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableIssueCount { get; set; }
        public int TotalIssueCount { get; set; }
        public long SizeOnDisk { get; set; }

        public decimal PercentOfIssues
        {
            get
            {
                if (IssueCount == 0)
                {
                    return 0;
                }

                return AvailableIssueCount / (decimal)IssueCount * 100;
            }
        }
    }

    public static class SeriesStatisticsResourceMapper
    {
        public static SeriesStatisticsResource ToResource(this SeriesStatistics model)
        {
            if (model == null)
            {
                return null;
            }

            return new SeriesStatisticsResource
            {
                IssueFileCount = model.ComicFileCount,
                IssueCount = model.IssueCount,
                AvailableIssueCount = model.AvailableIssueCount,
                TotalIssueCount = model.TotalIssueCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
