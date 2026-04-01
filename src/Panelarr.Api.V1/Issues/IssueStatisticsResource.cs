using NzbDrone.Core.SeriesStats;

namespace Panelarr.Api.V1.Issues
{
    public class IssueStatisticsResource
    {
        public int IssueFileCount { get; set; }
        public int IssueCount { get; set; }
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

                return IssueFileCount / (decimal)IssueCount * 100;
            }
        }
    }

    public static class IssueStatisticsResourceMapper
    {
        public static IssueStatisticsResource ToResource(this IssueStatistics model)
        {
            if (model == null)
            {
                return null;
            }

            return new IssueStatisticsResource
            {
                IssueFileCount = model.ComicFileCount,
                IssueCount = model.IssueCount,
                SizeOnDisk = model.SizeOnDisk,
                TotalIssueCount = model.TotalIssueCount
            };
        }
    }
}
