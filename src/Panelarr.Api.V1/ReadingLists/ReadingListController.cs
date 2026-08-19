using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

    public class ReadingListAddSeriesResource
    {
        public string ForeignSeriesId { get; set; }
        public string RootFolderPath { get; set; }
        public int QualityProfileId { get; set; }
        public bool Monitored { get; set; } = true;
    }

    public class ReadingListRemapSlotResource
    {
        // Links the slot to this library issue and rewrites the slot's
        // stored identity from it.
        public int? IssueId { get; set; }

        // Alternatively: backfills the slot's provider series id (manual
        // pick for an ambiguous provider-resolve candidate).
        public string ForeignSeriesId { get; set; }
    }

    public class ReadingListPushResultResource
    {
        public string ConnectionName { get; set; }
        public string Reader { get; set; }
        public bool Success { get; set; }
        public bool Updated { get; set; }
        public int MatchedCount { get; set; }
        public List<string> Unmatched { get; set; }
        public string ErrorMessage { get; set; }
    }

    [V1ApiController("readinglist")]
    public class ReadingListController : Controller
    {
        private readonly IReadingListService _readingListService;
        private readonly IReadingListPushService _pushService;
        private readonly IIssueService _issueService;
        private readonly IComicVineApiClient _comicVine;

        public ReadingListController(IReadingListService readingListService,
                             IReadingListPushService pushService,
                             IIssueService issueService,
                             IComicVineApiClient comicVine)
        {
            _readingListService = readingListService;
            _pushService = pushService;
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

        [HttpGet("export")]
        public IActionResult ExportAllCbl()
        {
            var lists = _readingListService.All();

            using var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var list in lists)
                {
                    var xml = _readingListService.ExportCbl(list.Id);
                    var baseName = SanitizeFileName(list.Name);
                    var entryName = usedNames.Add(baseName) ? baseName : $"{baseName} ({list.Id})";
                    usedNames.Add(entryName);

                    var entry = zip.CreateEntry($"{entryName}.cbl");
                    using var entryStream = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(xml);
                    entryStream.Write(bytes, 0, bytes.Length);
                }
            }

            return File(stream.ToArray(), "application/zip", $"reading-lists-{DateTime.UtcNow:yyyyMMdd}.zip");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);

            foreach (var c in name)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            var sanitized = builder.ToString().Trim();

            return sanitized.Length > 0 ? sanitized : "reading-list";
        }

        [HttpPost("{id:int}/addseries")]
        public IActionResult AddMissingSeries(int id, [FromBody] ReadingListAddSeriesResource resource)
        {
            if (string.IsNullOrWhiteSpace(resource?.ForeignSeriesId) || string.IsNullOrWhiteSpace(resource.RootFolderPath))
            {
                throw new BadRequestException("foreignSeriesId and rootFolderPath are required");
            }

            try
            {
                _readingListService.AddMissingSeries(id, resource.ForeignSeriesId, resource.RootFolderPath, resource.QualityProfileId, resource.Monitored);
            }
            catch (ArgumentException ex)
            {
                throw new BadRequestException(ex.Message);
            }

            return Accepted(new object());
        }

        [HttpPut("{id:int}/slots/{slotId:int}")]
        public ReadingListItemResource RemapSlot(int id, int slotId, [FromBody] ReadingListRemapSlotResource resource)
        {
            if (resource?.IssueId == null && string.IsNullOrWhiteSpace(resource?.ForeignSeriesId))
            {
                throw new BadRequestException("issueId or foreignSeriesId is required");
            }

            try
            {
                var slot = resource.IssueId != null
                    ? _readingListService.RemapSlot(id, slotId, resource.IssueId.Value)
                    : _readingListService.LinkSlotSeries(id, slotId, resource.ForeignSeriesId);

                return ToSlotResources(new List<ReadingListItem> { slot }).Single();
            }
            catch (ArgumentException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        [HttpPost("{id:int}/resolveprovider")]
        public ProviderResolveReport ResolveProvider(int id)
        {
            using var scope = NzbDrone.Core.MetadataSource.ComicVine.ComicVineRequestScope.Interactive();

            return _readingListService.ResolveMissingProviderIds(id);
        }

        [HttpPost("{id:int}/push")]
        public List<ReadingListPushResultResource> Push(int id)
        {
            // Ensures a 404 for unknown ids before any reader is contacted.
            _readingListService.Get(id);

            return _pushService.PushToReaders(id)
                .Select(r => new ReadingListPushResultResource
                {
                    ConnectionName = r.ConnectionName,
                    Reader = r.Reader,
                    Success = r.Success,
                    Updated = r.Updated,
                    MatchedCount = r.MatchedCount,
                    Unmatched = r.Unmatched,
                    ErrorMessage = r.ErrorMessage
                })
                .ToList();
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
