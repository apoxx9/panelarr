using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Arcs.Cbl;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;

namespace NzbDrone.Core.Arcs
{
    public interface IArcService
    {
        List<Arc> All();
        Arc Get(int id);
        List<ArcIssue> GetSlots(int arcId);
        Arc AddFromProvider(int cvStoryArcId, ArcType type, out int skippedCollectedEditions);
        Arc ImportCbl(string xml, ArcType type, out List<string> unresolved);
        string ExportCbl(int arcId);
        List<ArcIssue> Resolve(int arcId);
        void Delete(int id);
    }

    public class ArcService : IArcService
    {
        // TPB/HC/Omnibus pollution filter for CV arc imports: CV arcs freely
        // mix collected editions with the single issues (docs/story-arcs.md
        // decision: filter with a skipped-count note, matching ComicParser's
        // collected-edition vocabulary).
        private static readonly Regex CollectedEditionRegex = new Regex(
            @"\b(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus|Compendium|Collected)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IArcRepository _arcRepository;
        private readonly IArcIssueRepository _slotRepository;
        private readonly IComicVineApiClient _comicVine;
        private readonly IIssueService _issueService;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public ArcService(IArcRepository arcRepository,
                          IArcIssueRepository slotRepository,
                          IComicVineApiClient comicVine,
                          IIssueService issueService,
                          ISeriesService seriesService,
                          Logger logger)
        {
            _arcRepository = arcRepository;
            _slotRepository = slotRepository;
            _comicVine = comicVine;
            _issueService = issueService;
            _seriesService = seriesService;
            _logger = logger;
        }

        public List<Arc> All()
        {
            return _arcRepository.All().ToList();
        }

        public Arc Get(int id)
        {
            return _arcRepository.Get(id);
        }

        public List<ArcIssue> GetSlots(int arcId)
        {
            return _slotRepository.FindByArcId(arcId);
        }

        public Arc AddFromProvider(int cvStoryArcId, ArcType type, out int skippedCollectedEditions)
        {
            var foreignArcId = $"cv:{cvStoryArcId}";

            if (_arcRepository.FindByForeignArcId(foreignArcId) != null)
            {
                throw new ArgumentException("This arc has already been added");
            }

            var providerArc = _comicVine.GetStoryArc(cvStoryArcId);

            if (providerArc == null)
            {
                throw new ArgumentException($"ComicVine story arc {cvStoryArcId} not found");
            }

            var providerIssues = _comicVine.GetStoryArcIssues(cvStoryArcId);

            var singles = providerIssues
                .Where(i => !IsCollectedEdition(i))
                .OrderBy(i => i.CoverDate ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(i => i.Volume?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            skippedCollectedEditions = providerIssues.Count - singles.Count;

            var arc = _arcRepository.Insert(new Arc
            {
                Name = providerArc.Name,
                ForeignArcId = foreignArcId,
                Type = type,
                Publisher = providerArc.Publisher?.Name,
                Description = providerArc.Deck,
                Added = DateTime.UtcNow
            });

            var slots = singles.Select((issue, index) => new ArcIssue
            {
                ArcId = arc.Id,
                Position = index + 1,
                ForeignIssueId = $"cv:{issue.Id}",
                SeriesName = issue.Volume?.Name,
                IssueNumber = issue.IssueNumber,
                Year = ParseYear(issue.CoverDate)
            }).ToList();

            _slotRepository.InsertMany(slots);

            Resolve(arc.Id);

            _logger.Info("Added arc {0} from ComicVine: {1} issues ({2} collected editions skipped)", arc.Name, slots.Count, skippedCollectedEditions);

            return arc;
        }

        public Arc ImportCbl(string xml, ArcType type, out List<string> unresolved)
        {
            var list = CblFormat.Parse(xml);

            if (list.Name.IsNullOrWhiteSpace())
            {
                throw new InvalidCblFileException("Reading list has no <Name>");
            }

            if (list.Books.Empty())
            {
                throw new InvalidCblFileException("Reading list has no <Book> entries");
            }

            var arc = _arcRepository.Insert(new Arc
            {
                Name = list.Name,
                Type = type,
                Added = DateTime.UtcNow
            });

            var slots = list.Books.Select((book, index) => new ArcIssue
            {
                ArcId = arc.Id,
                Position = index + 1,
                ForeignIssueId = book.CvIssueId.HasValue ? $"cv:{book.CvIssueId.Value}" : null,
                SeriesName = book.Series,
                IssueNumber = book.Number,
                Volume = book.Volume,
                Year = book.Year
            }).ToList();

            _slotRepository.InsertMany(slots);

            var resolvedSlots = Resolve(arc.Id);

            unresolved = resolvedSlots
                .Where(s => s.IssueId == null)
                .Select(s => s.ForeignIssueId.IsNotNullOrWhiteSpace()
                    ? $"{s.SeriesName} #{s.IssueNumber}: series not in library"
                    : $"{s.SeriesName} #{s.IssueNumber}: no ComicVine id and no library match by name")
                .ToList();

            _logger.Info("Imported CBL {0}: {1} slots, {2} unresolved", arc.Name, slots.Count, unresolved.Count);

            return arc;
        }

        public string ExportCbl(int arcId)
        {
            var arc = _arcRepository.Get(arcId);
            var slots = _slotRepository.FindByArcId(arcId);

            // Resolved slots export with the library's own series naming, so
            // readers fed by Panelarr-managed files match by construction.
            var libraryIssues = GetLibraryIssues(slots);

            var books = slots.Select(slot =>
            {
                var book = new CblBook
                {
                    Series = slot.SeriesName,
                    Number = slot.IssueNumber,
                    Volume = slot.Volume,
                    Year = slot.Year,
                    CvIssueId = ParseForeignId(slot.ForeignIssueId)
                };

                if (slot.IssueId.HasValue && libraryIssues.TryGetValue(slot.IssueId.Value, out var issue))
                {
                    var series = issue.Series?.Value;

                    if (series != null)
                    {
                        book.Series = series.Name;
                        book.Volume = series.Metadata.Value.Year?.ToString() ?? book.Volume;
                        book.CvVolumeId = ParseForeignId(series.ForeignSeriesId);
                    }
                }

                return book;
            });

            return CblFormat.Write(arc.Name, books);
        }

        public List<ArcIssue> Resolve(int arcId)
        {
            var slots = _slotRepository.FindByArcId(arcId);
            var updated = new List<ArcIssue>();

            // Bust dangling links first: a slot pointing at an issue that no
            // longer exists silently unresolves (slots survive library churn;
            // there are deliberately no delete-event handlers to drift).
            var linkedIssues = GetLibraryIssues(slots);

            foreach (var slot in slots)
            {
                var resolvedId = ResolveSlot(slot, linkedIssues);

                if (slot.IssueId != resolvedId)
                {
                    slot.IssueId = resolvedId;
                    updated.Add(slot);
                }
            }

            if (updated.Any())
            {
                _slotRepository.UpdateMany(updated);
            }

            return slots;
        }

        public void Delete(int id)
        {
            _slotRepository.DeleteByArcId(id);
            _arcRepository.Delete(id);
        }

        private int? ResolveSlot(ArcIssue slot, Dictionary<int, Issue> linkedIssues)
        {
            // Keep a still-valid existing link.
            if (slot.IssueId.HasValue && linkedIssues.ContainsKey(slot.IssueId.Value))
            {
                return slot.IssueId;
            }

            // Tier 1: exact foreign id.
            if (slot.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                var byId = _issueService.FindById(slot.ForeignIssueId);

                if (byId != null)
                {
                    return byId.Id;
                }

                return null;
            }

            // Tier 2 (id-less CBLs): exact series name + issue number.
            if (slot.SeriesName.IsNullOrWhiteSpace() || slot.IssueNumber.IsNullOrWhiteSpace())
            {
                return null;
            }

            var series = _seriesService.FindByName(slot.SeriesName);

            if (series == null)
            {
                return null;
            }

            var issue = _issueService.GetIssuesBySeries(series.Id)
                .FirstOrDefault(i => string.Equals(i.IssueNumber, slot.IssueNumber, StringComparison.OrdinalIgnoreCase));

            return issue?.Id;
        }

        private Dictionary<int, Issue> GetLibraryIssues(List<ArcIssue> slots)
        {
            var ids = slots.Where(s => s.IssueId.HasValue).Select(s => s.IssueId.Value).Distinct().ToList();

            if (ids.Empty())
            {
                return new Dictionary<int, Issue>();
            }

            return _issueService.GetIssues(ids).ToDictionary(i => i.Id);
        }

        private static bool IsCollectedEdition(ComicVineArcIssue issue)
        {
            return CollectedEditionRegex.IsMatch(issue.Volume?.Name ?? string.Empty) ||
                   CollectedEditionRegex.IsMatch(issue.Name ?? string.Empty);
        }

        private static string ParseYear(string coverDate)
        {
            if (coverDate.IsNullOrWhiteSpace() || coverDate.Length < 4)
            {
                return null;
            }

            return coverDate.Substring(0, 4);
        }

        private static int? ParseForeignId(string foreignId)
        {
            if (foreignId.IsNullOrWhiteSpace() || !foreignId.StartsWith("cv:"))
            {
                return null;
            }

            return int.TryParse(foreignId.Substring(3), out var id) ? (int?)id : null;
        }
    }
}
