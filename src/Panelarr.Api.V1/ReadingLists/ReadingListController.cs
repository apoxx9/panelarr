using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.ReadingLists;
using NzbDrone.Core.ReadingLists.Cbl;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.ReadingLists
{
    public class ReadingListResource : RestResource
    {
        public string Name { get; set; }
        public string ForeignReadingListId { get; set; }
        public ReadingListType Type { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public DateTime Added { get; set; }
        public int SlotCount { get; set; }
        public int ResolvedCount { get; set; }
        public int HaveCount { get; set; }
    }

    public class ReadingListItemResource
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public int? IssueId { get; set; }
        public int? SeriesId { get; set; }
        public string SeriesTitleSlug { get; set; }
        public string ForeignIssueId { get; set; }
        public string ForeignSeriesId { get; set; }
        public string SeriesName { get; set; }
        public string IssueNumber { get; set; }
        public string Volume { get; set; }
        public string Year { get; set; }

        // have | missing | notInLibrary — computed live, never stored.
        public string Status { get; set; }
    }

    public class ReadingListDetailResource : ReadingListResource
    {
        public List<ReadingListItemResource> Slots { get; set; }
    }

    public class ReadingListSearchResource
    {
        public int CvStoryArcId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Deck { get; set; }
        public int IssueCount { get; set; }
    }

    public class ReadingListAddResource
    {
        public int CvStoryArcId { get; set; }
        public ReadingListType Type { get; set; } = ReadingListType.Arc;
    }

    public class ReadingListImportResource
    {
        public string Cbl { get; set; }
        public ReadingListType Type { get; set; } = ReadingListType.ReadingOrder;
    }

    public class ReadingListImportReportResource : RestResource
    {
        public string Name { get; set; }
        public int SlotCount { get; set; }
        public int UnresolvedCount { get; set; }
        public List<string> Unresolved { get; set; }
        public int SkippedCollectedEditions { get; set; }
    }

    [V1ApiController("readinglist")]
    public class ReadingListController : Controller
    {
        private readonly IReadingListService _readingListService;
        private readonly IIssueService _issueService;
        private readonly IComicVineApiClient _comicVine;

        public ReadingListController(IReadingListService readingListService,
                             IIssueService issueService,
                             IComicVineApiClient comicVine)
        {
            _readingListService = readingListService;
            _issueService = issueService;
            _comicVine = comicVine;
        }

        [HttpGet]
        public List<ReadingListResource> GetReadingLists()
        {
            return _readingListService.All()
                .Select(arc => ToResource(arc, _readingListService.GetSlots(arc.Id)))
                .ToList();
        }

        [HttpGet("{id:int}")]
        public ReadingListDetailResource GetReadingList(int id)
        {
            var arc = _readingListService.Get(id);
            var slots = _readingListService.Resolve(id);

            var resource = new ReadingListDetailResource();

            CopyArcFields(arc, slots, resource);
            resource.Slots = ToSlotResources(slots);

            return resource;
        }

        [HttpGet("search")]
        public List<ReadingListSearchResource> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new BadRequestException("query is required");
            }

            return _comicVine.SearchStoryArcs(query)
                .Select(a => new ReadingListSearchResource
                {
                    CvStoryArcId = a.Id,
                    Name = a.Name,
                    Publisher = a.Publisher?.Name,
                    Deck = a.Deck,
                    IssueCount = a.CountOfIssueAppearances
                })
                .ToList();
        }

        [HttpPost]
        public ActionResult<ReadingListImportReportResource> Add([FromBody] ReadingListAddResource resource)
        {
            try
            {
                var arc = _readingListService.AddFromProvider(resource.CvStoryArcId, resource.Type, out var skipped);
                var slots = _readingListService.GetSlots(arc.Id);

                return new ReadingListImportReportResource
                {
                    Id = arc.Id,
                    Name = arc.Name,
                    SlotCount = slots.Count,
                    UnresolvedCount = slots.Count(s => s.IssueId == null),
                    Unresolved = new List<string>(),
                    SkippedCollectedEditions = skipped
                };
            }
            catch (ArgumentException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPost("import")]
        public ActionResult<ReadingListImportReportResource> ImportCbl([FromBody] ReadingListImportResource resource)
        {
            if (string.IsNullOrWhiteSpace(resource?.Cbl))
            {
                throw new BadRequestException("cbl content is required");
            }

            try
            {
                var arc = _readingListService.ImportCbl(resource.Cbl, resource.Type, out var unresolved);
                var slots = _readingListService.GetSlots(arc.Id);

                return new ReadingListImportReportResource
                {
                    Id = arc.Id,
                    Name = arc.Name,
                    SlotCount = slots.Count,
                    UnresolvedCount = unresolved.Count,
                    Unresolved = unresolved
                };
            }
            catch (InvalidCblFileException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpGet("{id:int}/export")]
        public IActionResult ExportCbl(int id)
        {
            var arc = _readingListService.Get(id);
            var xml = _readingListService.ExportCbl(id);

            return File(Encoding.UTF8.GetBytes(xml), "application/xml", $"{arc.Name}.cbl");
        }

        [RestDeleteById]
        public void DeleteReadingList(int id)
        {
            _readingListService.Delete(id);
        }

        private ReadingListResource ToResource(ReadingList arc, List<ReadingListItem> slots)
        {
            var resource = new ReadingListResource();
            CopyArcFields(arc, slots, resource);
            return resource;
        }

        private void CopyArcFields(ReadingList arc, List<ReadingListItem> slots, ReadingListResource resource)
        {
            var haveCount = CountHave(slots);

            resource.Id = arc.Id;
            resource.Name = arc.Name;
            resource.ForeignReadingListId = arc.ForeignReadingListId;
            resource.Type = arc.Type;
            resource.Publisher = arc.Publisher;
            resource.Description = arc.Description;
            resource.Added = arc.Added;
            resource.SlotCount = slots.Count;
            resource.ResolvedCount = slots.Count(s => s.IssueId.HasValue);
            resource.HaveCount = haveCount;
        }

        private int CountHave(List<ReadingListItem> slots)
        {
            var issues = GetIssues(slots);

            return slots.Count(s => s.IssueId.HasValue &&
                                    issues.TryGetValue(s.IssueId.Value, out var issue) &&
                                    (issue.ComicFiles?.Value?.Any() ?? false));
        }

        private List<ReadingListItemResource> ToSlotResources(List<ReadingListItem> slots)
        {
            var issues = GetIssues(slots);

            return slots.Select(slot =>
            {
                var resource = new ReadingListItemResource
                {
                    Id = slot.Id,
                    Position = slot.Position,
                    IssueId = slot.IssueId,
                    ForeignIssueId = slot.ForeignIssueId,
                    ForeignSeriesId = slot.ForeignSeriesId,
                    SeriesName = slot.SeriesName,
                    IssueNumber = slot.IssueNumber,
                    Volume = slot.Volume,
                    Year = slot.Year,
                    Status = "notInLibrary"
                };

                if (slot.IssueId.HasValue && issues.TryGetValue(slot.IssueId.Value, out var issue))
                {
                    var series = issue.Series?.Value;

                    resource.SeriesId = series?.Id;
                    resource.SeriesTitleSlug = series?.Metadata?.Value?.TitleSlug;
                    resource.SeriesName = series?.Name ?? slot.SeriesName;
                    resource.Status = (issue.ComicFiles?.Value?.Any() ?? false) ? "have" : "missing";
                }

                return resource;
            }).ToList();
        }

        private Dictionary<int, Issue> GetIssues(List<ReadingListItem> slots)
        {
            var ids = slots.Where(s => s.IssueId.HasValue).Select(s => s.IssueId.Value).Distinct().ToList();

            return ids.Any()
                ? _issueService.GetIssues(ids).ToDictionary(i => i.Id)
                : new Dictionary<int, Issue>();
        }
    }
}
