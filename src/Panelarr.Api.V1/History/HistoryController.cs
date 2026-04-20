using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.Series;
using Panelarr.Http;
using Panelarr.Http.Extensions;

namespace Panelarr.Api.V1.History
{
    [V1ApiController]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;
        private readonly ISeriesService _seriesService;

        public HistoryController(IHistoryService historyService,
                             ICustomFormatCalculationService formatCalculator,
                             IUpgradableSpecification upgradableSpecification,
                             IFailedDownloadService failedDownloadService,
                             ISeriesService seriesService)
        {
            _historyService = historyService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
            _seriesService = seriesService;
        }

        protected HistoryResource MapToResource(EntityHistory model, bool includeSeries, bool includeIssue)
        {
            var resource = model.ToResource(_formatCalculator);

            if (includeSeries)
            {
                resource.Series = model.Series.ToResource();
            }

            if (includeIssue)
            {
                resource.Issue = model.Issue.ToResource();
            }

            if (model.Series != null)
            {
                resource.QualityCutoffNotMet = _upgradableSpecification.QualityCutoffNotMet(model.Series.QualityProfile.Value, model.Quality);
            }

            return resource;
        }

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<HistoryResource> GetHistory([FromQuery] PagingRequestResource paging, bool includeSeries, bool includeIssue, [FromQuery(Name = "eventType")] int[] eventTypes, int? issueId, string downloadId)
        {
            var pagingResource = new PagingResource<HistoryResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, EntityHistory>("date", SortDirection.Descending);

            if (eventTypes != null && eventTypes.Any())
            {
                pagingSpec.FilterExpressions.Add(v => eventTypes.Contains((int)v.EventType));
            }

            if (issueId.HasValue)
            {
                pagingSpec.FilterExpressions.Add(h => h.IssueId == issueId);
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h, includeSeries, includeIssue));
        }

        [HttpGet("since")]
        public List<HistoryResource> GetHistorySince(DateTime date, EntityHistoryEventType? eventType = null, bool includeSeries = false, bool includeIssue = false)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeSeries, includeIssue)).ToList();
        }

        [HttpGet("series")]
        public List<HistoryResource> GetSeriesHistory(int seriesId, int? issueId = null, EntityHistoryEventType? eventType = null, bool includeSeries = false, bool includeIssue = false)
        {
            var series = _seriesService.GetSeries(seriesId);

            if (issueId.HasValue)
            {
                return _historyService.GetByIssue(issueId.Value, eventType).Select(h =>
                {
                    h.Series = series;

                    return MapToResource(h, includeSeries, includeIssue);
                }).ToList();
            }

            return _historyService.GetBySeries(seriesId, eventType).Select(h =>
            {
                h.Series = series;

                return MapToResource(h, includeSeries, includeIssue);
            }).ToList();
        }

        [HttpPost("failed/{id}")]
        public object MarkAsFailed([FromRoute] int id)
        {
            _failedDownloadService.MarkAsFailed(id);
            return new { };
        }
    }
}
