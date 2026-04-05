using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.SeriesStats;
using Panelarr.Http;

namespace Panelarr.Api.V1.Series
{
    /// <summary>GET /api/v1/series/stats — library overview statistics.</summary>
    [V1ApiController("series")]
    public class LibraryStatsController : Controller
    {
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IMediaFileService _mediaFileService;
        private readonly ISeriesStatisticsService _seriesStatisticsService;

        public LibraryStatsController(ISeriesService seriesService,
                                      IIssueService issueService,
                                      IMediaFileService mediaFileService,
                                      ISeriesStatisticsService seriesStatisticsService)
        {
            _seriesService = seriesService;
            _issueService = issueService;
            _mediaFileService = mediaFileService;
            _seriesStatisticsService = seriesStatisticsService;
        }

        [HttpGet("stats")]
        public LibraryStatsResource GetStats()
        {
            var allStats = _seriesStatisticsService.SeriesStatistics();
            var allSeries = _seriesService.GetAllSeries();

            var totalSeries = allSeries.Count;
            var totalIssues = allStats.Sum(s => s.TotalIssueCount);
            var haveIssues = allStats.Sum(s => s.ComicFileCount);
            var missingIssues = totalIssues - haveIssues;
            var totalSizeBytes = allStats.Sum(s => s.SizeOnDisk);

            return new LibraryStatsResource
            {
                TotalSeries = totalSeries,
                TotalIssues = totalIssues,
                HaveIssues = haveIssues,
                MissingIssues = missingIssues > 0 ? missingIssues : 0,
                TotalSizeBytes = totalSizeBytes
            };
        }
    }

    public class LibraryStatsResource
    {
        public int TotalSeries { get; set; }
        public int TotalIssues { get; set; }
        public int HaveIssues { get; set; }
        public int MissingIssues { get; set; }
        public long TotalSizeBytes { get; set; }
    }
}
