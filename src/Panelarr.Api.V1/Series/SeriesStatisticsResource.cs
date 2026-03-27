using NzbDrone.Core.SeriesStats;

namespace Panelarr.Api.V1.Series
{
    public class SeriesStatisticsResource
    {
        public int ComicFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableBookCount { get; set; }
        public int TotalBookCount { get; set; }
        public long SizeOnDisk { get; set; }

        public decimal PercentOfBooks
        {
            get
            {
                if (IssueCount == 0)
                {
                    return 0;
                }

                return AvailableBookCount / (decimal)IssueCount * 100;
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
                ComicFileCount = model.ComicFileCount,
                IssueCount = model.IssueCount,
                AvailableBookCount = model.AvailableBookCount,
                TotalBookCount = model.TotalBookCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
