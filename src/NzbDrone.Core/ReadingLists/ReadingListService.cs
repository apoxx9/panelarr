using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;
using NzbDrone.Core.Parser;
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
        string ExportCbl(int readingListId, bool resolvedOnly = false);
        List<ReadingListItem> Resolve(int readingListId);
        ReadingListItem RemapSlot(int readingListId, int slotId, int issueId);
        ReadingListItem LinkSlotSeries(int readingListId, int slotId, string foreignSeriesId);
        ProviderResolveReport ResolveMissingProviderIds(int readingListId);
        Series AddMissingSeries(int readingListId, string foreignSeriesId, string rootFolderPath, int qualityProfileId, bool monitored);
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
        private readonly IAddSeriesService _addSeriesService;
        private readonly IRefreshSeriesService _refreshSeriesService;
        private readonly Logger _logger;

        public ReadingListService(IReadingListRepository arcRepository,
                          IReadingListItemRepository slotRepository,
                          IComicVineApiClient comicVine,
                          IIssueService issueService,
                          ISeriesService seriesService,
                          IAddSeriesService addSeriesService,
                          IRefreshSeriesService refreshSeriesService,
                          Logger logger)
        {
            _listRepository = arcRepository;
            _slotRepository = slotRepository;
            _comicVine = comicVine;
            _issueService = issueService;
            _seriesService = seriesService;
            _addSeriesService = addSeriesService;
            _refreshSeriesService = refreshSeriesService;
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

        public string ExportCbl(int readingListId, bool resolvedOnly = false)
        {
            var list = _listRepository.Get(readingListId);
            var slots = _slotRepository.FindByReadingListId(readingListId);

            // Readers can only match owned books; unresolved slots make e.g.
            // Kavita reject the whole list ("series missing"), so pushes
            // export the resolved subset.
            if (resolvedOnly)
            {
                slots = slots.Where(s => s.IssueId.HasValue).ToList();
            }

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
                    // The library's identity wins for resolved slots — including
                    // the issue id, so a list resolved past a bad community id
                    // exports as a REPAIRED file.
                    book.CvIssueId = ParseForeignId(issue.ForeignIssueId) ?? book.CvIssueId;

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

        public ReadingListItem RemapSlot(int readingListId, int slotId, int issueId)
        {
            // There is deliberately no unlink: resolve is lazy and would just
            // re-match the slot on the next view. A wrong link is fixed by
            // remapping to the right issue.
            var slot = _slotRepository.Get(slotId);

            if (slot.ReadingListId != readingListId)
            {
                throw new ArgumentException("Slot does not belong to this reading list");
            }

            var issue = _issueService.GetIssue(issueId);
            var series = issue.Series?.Value;

            // A manual remap is an explicit correction: the slot's stored
            // identity is rewritten from the library issue, so the fix is
            // durable and CBL export carries the corrected ids (the user's
            // fix wins over the file's claim).
            slot.IssueId = issue.Id;
            slot.ForeignIssueId = issue.ForeignIssueId;
            slot.IssueNumber = issue.IssueNumber;

            if (series != null)
            {
                slot.ForeignSeriesId = series.ForeignSeriesId;
                slot.SeriesName = series.Name;
                slot.Volume = series.Metadata.Value.Year?.ToString() ?? slot.Volume;
            }

            if (issue.ReleaseDate.HasValue)
            {
                slot.Year = issue.ReleaseDate.Value.Year.ToString();
            }

            _slotRepository.Update(slot);

            _logger.Info("Remapped reading list slot {0} to issue {1} ({2} #{3})", slot.Position, issue.Id, slot.SeriesName, slot.IssueNumber);

            return slot;
        }

        public Series AddMissingSeries(int readingListId, string foreignSeriesId, string rootFolderPath, int qualityProfileId, bool monitored)
        {
            // Explicit, user-initiated only — imports never touch the library
            // (docs/story-arcs.md). Mirrors the proven staging-import path:
            // add without refresh, then refresh synchronously so the slots
            // can resolve against the new series' issues immediately.
            var series = _seriesService.FindById(foreignSeriesId);

            if (series == null)
            {
                series = _addSeriesService.AddSeries(new Series
                {
                    Metadata = new SeriesMetadata { ForeignSeriesId = foreignSeriesId },
                    RootFolderPath = rootFolderPath,
                    QualityProfileId = qualityProfileId,
                    Monitored = monitored,
                    MonitorNewItems = monitored ? NewItemMonitorTypes.All : NewItemMonitorTypes.None,
                    AddOptions = new AddSeriesOptions
                    {
                        SearchForMissingIssues = false,
                        Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                    }
                }, doRefresh: false);

                _refreshSeriesService.RefreshSeries(new List<int> { series.Id }, true, Messaging.Commands.CommandTrigger.Manual);
            }

            Resolve(readingListId);

            return series;
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

            // Tier 1: exact foreign id. A miss FALLS THROUGH to name matching:
            // community CBLs carry wrong/stale ids (TPB records, duplicate CV
            // volumes for the same book), and a dangling id shouldn't beat an
            // exact name+number match — Kavita tiers the same way.
            if (slot.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                var byId = _issueService.FindById(slot.ForeignIssueId);

                if (byId != null)
                {
                    return byId.Id;
                }
            }

            // Tier 2: exact series name + issue number, year-disambiguated.
            if (slot.SeriesName.IsNullOrWhiteSpace() || slot.IssueNumber.IsNullOrWhiteSpace())
            {
                return null;
            }

            var series = PickSeriesByName(slot);

            if (series == null)
            {
                return null;
            }

            var issue = _issueService.GetIssuesBySeries(series.Id)
                .FirstOrDefault(i => string.Equals(i.IssueNumber, slot.IssueNumber, StringComparison.OrdinalIgnoreCase));

            return issue?.Id;
        }

        public ReadingListItem LinkSlotSeries(int readingListId, int slotId, string foreignSeriesId)
        {
            var slot = _slotRepository.FindByReadingListId(readingListId).FirstOrDefault(s => s.Id == slotId);

            if (slot == null)
            {
                throw new ArgumentException($"Slot {slotId} not found in reading list {readingListId}");
            }

            slot.ForeignSeriesId = foreignSeriesId;
            _slotRepository.UpdateMany(new List<ReadingListItem> { slot });

            return slot;
        }

        public ProviderResolveReport ResolveMissingProviderIds(int readingListId)
        {
            var slots = _slotRepository.FindByReadingListId(readingListId);
            var targets = slots
                .Where(s => s.IssueId == null &&
                            s.ForeignSeriesId.IsNullOrWhiteSpace() &&
                            s.SeriesName.IsNotNullOrWhiteSpace())
                .ToList();

            var report = new ProviderResolveReport { SlotsConsidered = targets.Count };
            var toUpdate = new List<ReadingListItem>();

            // One provider search per distinct name; the CV client throttles
            // itself, so per-list scope keeps this inside the rate budget.
            foreach (var group in targets.GroupBy(s => s.SeriesName))
            {
                List<ComicVineVolumeSummary> results;

                try
                {
                    results = _comicVine.SearchVolumes(group.Key) ?? new List<ComicVineVolumeSummary>();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Provider search failed for {0}", group.Key);
                    report.NotFound.Add($"{group.Key} (search failed)");
                    continue;
                }

                var exact = results.Where(r => ProviderNamesMatch(r.Name, group.Key)).ToList();

                if (exact.Count == 1)
                {
                    foreach (var slot in group)
                    {
                        slot.ForeignSeriesId = $"cv:{exact[0].Id}";
                        toUpdate.Add(slot);
                    }

                    report.Linked += group.Count();
                }
                else if (results.Count == 0)
                {
                    report.NotFound.Add(group.Key);
                }
                else
                {
                    // Multiple exact matches, or fuzzy-only results: never
                    // guess a provider id — surface candidates for the user
                    var pool = exact.Count > 0 ? exact : results;

                    report.Ambiguous.Add(new ProviderResolveAmbiguity
                    {
                        SeriesName = group.Key,
                        SlotIds = group.Select(s => s.Id).ToList(),
                        Candidates = pool.Take(5).Select(r => new ProviderResolveCandidate
                        {
                            ForeignSeriesId = $"cv:{r.Id}",
                            Name = r.Name,
                            Year = r.StartYear,
                            Publisher = r.Publisher?.Name
                        }).ToList()
                    });
                }
            }

            if (toUpdate.Any())
            {
                _slotRepository.UpdateMany(toUpdate);
            }

            _logger.Info("Provider resolve for list {0}: {1} slots linked, {2} ambiguous, {3} not found of {4} considered", readingListId, report.Linked, report.Ambiguous.Count, report.NotFound.Count, report.SlotsConsidered);

            return report;
        }

        private static bool ProviderNamesMatch(string providerName, string slotName)
        {
            var a = providerName.CleanSeriesName();
            var b = slotName.CleanSeriesName();

            if (a == b)
            {
                return true;
            }

            // Same leading-The tolerance as library-side slot resolution
            static string StripThe(string s) => s.StartsWith("the", StringComparison.Ordinal) ? s.Substring(3) : s;

            return StripThe(a) == StripThe(b);
        }

        private Series PickSeriesByName(ReadingListItem slot)
        {
            var candidates = _seriesService.FindAllByName(slot.SeriesName);

            // Name normalization deliberately keeps a LEADING "The" ("The
            // Walking Dead"), but providers are inconsistent about it across
            // volumes of one line (observed: 7 of 22 ASM Epic Collections
            // titled "The Amazing..."). On an exact miss, retry the single
            // The-toggled variant.
            if (candidates.Count == 0)
            {
                var variant = slot.SeriesName.StartsWith("The ", StringComparison.OrdinalIgnoreCase)
                    ? slot.SeriesName.Substring(4)
                    : $"The {slot.SeriesName}";

                candidates = _seriesService.FindAllByName(variant);
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            // Multiple same-named series (e.g. Power Rangers 2016 vs 2020):
            // only a year hint may disambiguate — never guess. CBL convention
            // puts the series start year in Volume; Year is the book's year.
            foreach (var hint in new[] { slot.Volume, slot.Year })
            {
                if (int.TryParse(hint, out var year))
                {
                    var byYear = candidates.Where(c => c.Metadata.Value.Year == year).ToList();

                    if (byYear.Count == 1)
                    {
                        return byYear[0];
                    }
                }
            }

            return null;
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
