using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Arcs;
using NzbDrone.Core.Arcs.Cbl;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Arcs
{
    public class ArcResource : RestResource
    {
        public string Name { get; set; }
        public string ForeignArcId { get; set; }
        public ArcType Type { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public DateTime Added { get; set; }
        public int SlotCount { get; set; }
        public int ResolvedCount { get; set; }
        public int HaveCount { get; set; }
    }

    public class ArcSlotResource
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public int? IssueId { get; set; }
        public int? SeriesId { get; set; }
        public string SeriesTitleSlug { get; set; }
        public string ForeignIssueId { get; set; }
        public string SeriesName { get; set; }
        public string IssueNumber { get; set; }
        public string Volume { get; set; }
        public string Year { get; set; }

        // have | missing | notInLibrary — computed live, never stored.
        public string Status { get; set; }
    }

    public class ArcDetailResource : ArcResource
    {
        public List<ArcSlotResource> Slots { get; set; }
    }

    public class ArcSearchResource
    {
        public int CvStoryArcId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string Deck { get; set; }
        public int IssueCount { get; set; }
    }

    public class ArcAddResource
    {
        public int CvStoryArcId { get; set; }
        public ArcType Type { get; set; } = ArcType.Arc;
    }

    public class ArcImportResource
    {
        public string Cbl { get; set; }
        public ArcType Type { get; set; } = ArcType.ReadingOrder;
    }

    public class ArcImportReportResource : RestResource
    {
        public string Name { get; set; }
        public int SlotCount { get; set; }
        public int UnresolvedCount { get; set; }
        public List<string> Unresolved { get; set; }
        public int SkippedCollectedEditions { get; set; }
    }

    [V1ApiController("arc")]
    public class ArcController : Controller
    {
        private readonly IArcService _arcService;
        private readonly IIssueService _issueService;
        private readonly IComicVineApiClient _comicVine;

        public ArcController(IArcService arcService,
                             IIssueService issueService,
                             IComicVineApiClient comicVine)
        {
            _arcService = arcService;
            _issueService = issueService;
            _comicVine = comicVine;
        }

        [HttpGet]
        public List<ArcResource> GetArcs()
        {
            return _arcService.All()
                .Select(arc => ToResource(arc, _arcService.GetSlots(arc.Id)))
                .ToList();
        }

        [HttpGet("{id:int}")]
        public ArcDetailResource GetArc(int id)
        {
            var arc = _arcService.Get(id);
            var slots = _arcService.Resolve(id);

            var resource = new ArcDetailResource();

            CopyArcFields(arc, slots, resource);
            resource.Slots = ToSlotResources(slots);

            return resource;
        }

        [HttpGet("search")]
        public List<ArcSearchResource> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new BadRequestException("query is required");
            }

            return _comicVine.SearchStoryArcs(query)
                .Select(a => new ArcSearchResource
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
        public ActionResult<ArcImportReportResource> Add([FromBody] ArcAddResource resource)
        {
            try
            {
                var arc = _arcService.AddFromProvider(resource.CvStoryArcId, resource.Type, out var skipped);
                var slots = _arcService.GetSlots(arc.Id);

                return new ArcImportReportResource
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
        public ActionResult<ArcImportReportResource> ImportCbl([FromBody] ArcImportResource resource)
        {
            if (string.IsNullOrWhiteSpace(resource?.Cbl))
            {
                throw new BadRequestException("cbl content is required");
            }

            try
            {
                var arc = _arcService.ImportCbl(resource.Cbl, resource.Type, out var unresolved);
                var slots = _arcService.GetSlots(arc.Id);

                return new ArcImportReportResource
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
            var arc = _arcService.Get(id);
            var xml = _arcService.ExportCbl(id);

            return File(Encoding.UTF8.GetBytes(xml), "application/xml", $"{arc.Name}.cbl");
        }

        [RestDeleteById]
        public void DeleteArc(int id)
        {
            _arcService.Delete(id);
        }

        private ArcResource ToResource(Arc arc, List<ArcIssue> slots)
        {
            var resource = new ArcResource();
            CopyArcFields(arc, slots, resource);
            return resource;
        }

        private void CopyArcFields(Arc arc, List<ArcIssue> slots, ArcResource resource)
        {
            var haveCount = CountHave(slots);

            resource.Id = arc.Id;
            resource.Name = arc.Name;
            resource.ForeignArcId = arc.ForeignArcId;
            resource.Type = arc.Type;
            resource.Publisher = arc.Publisher;
            resource.Description = arc.Description;
            resource.Added = arc.Added;
            resource.SlotCount = slots.Count;
            resource.ResolvedCount = slots.Count(s => s.IssueId.HasValue);
            resource.HaveCount = haveCount;
        }

        private int CountHave(List<ArcIssue> slots)
        {
            var issues = GetIssues(slots);

            return slots.Count(s => s.IssueId.HasValue &&
                                    issues.TryGetValue(s.IssueId.Value, out var issue) &&
                                    (issue.ComicFiles?.Value?.Any() ?? false));
        }

        private List<ArcSlotResource> ToSlotResources(List<ArcIssue> slots)
        {
            var issues = GetIssues(slots);

            return slots.Select(slot =>
            {
                var resource = new ArcSlotResource
                {
                    Id = slot.Id,
                    Position = slot.Position,
                    IssueId = slot.IssueId,
                    ForeignIssueId = slot.ForeignIssueId,
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

        private Dictionary<int, Issue> GetIssues(List<ArcIssue> slots)
        {
            var ids = slots.Where(s => s.IssueId.HasValue).Select(s => s.IssueId.Value).Distinct().ToList();

            return ids.Any()
                ? _issueService.GetIssues(ids).ToDictionary(i => i.Id)
                : new Dictionary<int, Issue>();
        }
    }
}
