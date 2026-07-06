using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.ReadingLists.Cbl;

namespace NzbDrone.Core.ReadingLists
{
    public interface IReadingListService
    {
        List<ReadingList> All();
        ReadingList Get(int id);
        List<ReadingListItem> GetSlots(int readingListId);
        ReadingList AddFromProvider(int cvStoryArcId, ReadingListType type, out int skippedCollectedEditions);
        ReadingList ImportCbl(string xml, ReadingListType type, out List<string> unresolved);
        string ExportCbl(int readingListId);
        List<ReadingListItem> Resolve(int readingListId);
        void Delete(int id);
    }

    public class ReadingListService : IReadingListService
    {
        // TPB/HC/Omnibus pollution filter for CV arc imports: CV arcs freely
        // mix collected editions with the single issues (docs/story-arcs.md
        // decision: filter with a skipped-count note, matching ComicParser's
        // collected-edition vocabulary).
        private static readonly Regex CollectedEditionRegex = new Regex(
            @"\b(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus|Compendium|Collected)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IReadingListRepository _listRepository;
        private readonly IReadingListItemRepository _slotRepository;
        private readonly IComicVineApiClient _comicVine;
        private readonly IIssueService _issueService;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public ReadingListService(IReadingListRepository arcRepository,
                          IReadingListItemRepository slotRepository,
                          IComicVineApiClient comicVine,
                          IIssueService issueService,
                          ISeriesService seriesService,
                          Logger logger)
        {
            _listRepository = arcRepository;
            _slotRepository = slotRepository;
            _comicVine = comicVine;
            _issueService = issueService;
            _seriesService = seriesService;
            _logger = logger;
        }

        public List<ReadingList> All()
        {
            return _listRepository.All().ToList();
        }

        public ReadingList Get(int id)
        {
            return _listRepository.Get(id);
        }

        public List<ReadingListItem> GetSlots(int readingListId)
        {
            return _slotRepository.FindByReadingListId(readingListId);
        }

        public ReadingList AddFromProvider(int cvStoryArcId, ReadingListType type, out int skippedCollectedEditions)
        {
            var foreignReadingListId = $"cv:{cvStoryArcId}";

            if (_listRepository.FindByForeignReadingListId(foreignReadingListId) != null)
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

            var list = _listRepository.Insert(new ReadingList
            {
                Name = providerArc.Name,
                ForeignReadingListId = foreignReadingListId,
                Type = type,
                Publisher = providerArc.Publisher?.Name,
                Description = providerArc.Deck,
                Added = DateTime.UtcNow
            });

            var slots = singles.Select((issue, index) => new ReadingListItem
            {
                ReadingListId = list.Id,
                Position = index + 1,
                ForeignIssueId = $"cv:{issue.Id}",
                ForeignSeriesId = issue.Volume != null ? $"cv:{issue.Volume.Id}" : null,
                SeriesName = issue.Volume?.Name,
                IssueNumber = issue.IssueNumber,
                Year = ParseYear(issue.CoverDate)
            }).ToList();

            _slotRepository.InsertMany(slots);

            Resolve(list.Id);

            _logger.Info("Added reading list {0} from ComicVine: {1} issues ({2} collected editions skipped)", list.Name, slots.Count, skippedCollectedEditions);

            return list;
        }

        public ReadingList ImportCbl(string xml, ReadingListType type, out List<string> unresolved)
        {
            var parsed = CblFormat.Parse(xml);

            if (parsed.Name.IsNullOrWhiteSpace())
            {
                throw new InvalidCblFileException("Reading list has no <Name>");
            }

            if (parsed.Books.Empty())
            {
                throw new InvalidCblFileException("Reading list has no <Book> entries");
            }

            var list = _listRepository.Insert(new ReadingList
            {
                Name = parsed.Name,
                Type = type,
                Added = DateTime.UtcNow
            });

            var slots = parsed.Books.Select((book, index) => new ReadingListItem
            {
                ReadingListId = list.Id,
                Position = index + 1,
                ForeignIssueId = book.CvIssueId.HasValue ? $"cv:{book.CvIssueId.Value}" : null,
                ForeignSeriesId = book.CvVolumeId.HasValue ? $"cv:{book.CvVolumeId.Value}" : null,
                SeriesName = book.Series,
                IssueNumber = book.Number,
                Volume = book.Volume,
                Year = book.Year
            }).ToList();

            _slotRepository.InsertMany(slots);

            var resolvedSlots = Resolve(list.Id);

            unresolved = resolvedSlots
                .Where(s => s.IssueId == null)
                .Select(s => s.ForeignIssueId.IsNotNullOrWhiteSpace()
                    ? $"{s.SeriesName} #{s.IssueNumber}: series not in library"
                    : $"{s.SeriesName} #{s.IssueNumber}: no ComicVine id and no library match by name")
                .ToList();

            _logger.Info("Imported CBL {0}: {1} slots, {2} unresolved", list.Name, slots.Count, unresolved.Count);

            return list;
        }

        public string ExportCbl(int readingListId)
        {
            var list = _listRepository.Get(readingListId);
            var slots = _slotRepository.FindByReadingListId(readingListId);

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
                    CvIssueId = ParseForeignId(slot.ForeignIssueId),
                    CvVolumeId = ParseForeignId(slot.ForeignSeriesId)
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

            return CblFormat.Write(list.Name, books);
        }

        public List<ReadingListItem> Resolve(int readingListId)
        {
            var slots = _slotRepository.FindByReadingListId(readingListId);
            var updated = new List<ReadingListItem>();

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
            _slotRepository.DeleteByReadingListId(id);
            _listRepository.Delete(id);
        }

        private int? ResolveSlot(ReadingListItem slot, Dictionary<int, Issue> linkedIssues)
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

        private Dictionary<int, Issue> GetLibraryIssues(List<ReadingListItem> slots)
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
