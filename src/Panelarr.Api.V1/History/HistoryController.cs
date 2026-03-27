using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using Panelarr.Api.V1.Books;
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
        private readonly ISeriesService _authorService;

        public HistoryController(IHistoryService historyService,
                             ICustomFormatCalculationService formatCalculator,
                             IUpgradableSpecification upgradableSpecification,
                             IFailedDownloadService failedDownloadService,
                             ISeriesService authorService)
        {
            _historyService = historyService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
            _authorService = authorService;
        }

        protected HistoryResource MapToResource(EntityHistory model, bool includeSeries, bool includeBook)
        {
            var resource = model.ToResource(_formatCalculator);

            if (includeSeries)
            {
                resource.Series = model.Series.ToResource();
            }

            if (includeBook)
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
        public PagingResource<HistoryResource> GetHistory([FromQuery] PagingRequestResource paging, bool includeSeries, bool includeBook, [FromQuery(Name = "eventType")] int[] eventTypes, int? bookId, string downloadId)
        {
            var pagingResource = new PagingResource<HistoryResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, EntityHistory>("date", SortDirection.Descending);

            if (eventTypes != null && eventTypes.Any())
            {
                pagingSpec.FilterExpressions.Add(v => eventTypes.Contains((int)v.EventType));
            }

            if (bookId.HasValue)
            {
                pagingSpec.FilterExpressions.Add(h => h.IssueId == bookId);
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h, includeSeries, includeBook));
        }

        [HttpGet("since")]
        public List<HistoryResource> GetHistorySince(DateTime date, EntityHistoryEventType? eventType = null, bool includeSeries = false, bool includeBook = false)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeSeries, includeBook)).ToList();
        }

        [HttpGet("author")]
        public List<HistoryResource> GetSeriesHistory(int authorId, int? bookId = null, EntityHistoryEventType? eventType = null, bool includeSeries = false, bool includeBook = false)
        {
            var author = _authorService.GetSeries(authorId);

            if (bookId.HasValue)
            {
                return _historyService.GetByBook(bookId.Value, eventType).Select(h =>
                {
                    h.Series = author;

                    return MapToResource(h, includeSeries, includeBook);
                }).ToList();
            }

            return _historyService.GetBySeries(authorId, eventType).Select(h =>
            {
                h.Series = author;

                return MapToResource(h, includeSeries, includeBook);
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
