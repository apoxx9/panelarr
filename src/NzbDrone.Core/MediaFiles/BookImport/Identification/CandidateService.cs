using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public interface ICandidateService
    {
        List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting);
        IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides);
    }

    public class CandidateService : ICandidateService
    {
        private readonly ISearchForNewBook _bookSearchService;
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public CandidateService(ISearchForNewBook bookSearchService,
                                ISeriesService authorService,
                                IBookService bookService,
                                IMediaFileService mediaFileService,
                                Logger logger)
        {
            _bookSearchService = bookSearchService;
            _authorService = authorService;
            _bookService = bookService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Generally author, issue and release are null.  But if they're not then limit candidates appropriately.
            // We've tried to make sure that tracks are all for a single release.
            List<CandidateEdition> candidateReleases;

            // if we have a Issue ID, use that
            Issue tagMbidRelease = null;
            List<CandidateEdition> tagCandidate = null;

            // TODO: select by ISBN?
            // var releaseIds = localEdition.LocalTracks.Select(x => x.FileTrackInfo.ReleaseMBId).Distinct().ToList();
            // if (releaseIds.Count == 1 && releaseIds[0].IsNotNullOrWhiteSpace())
            // {
            //     _logger.Debug("Selecting release from consensus ForeignReleaseId [{0}]", releaseIds[0]);
            //     tagMbidRelease = _releaseService.GetReleaseByForeignReleaseId(releaseIds[0], true);

            //     if (tagMbidRelease != null)
            //     {
            //         tagCandidate = GetDbCandidatesByRelease(new List<IssueRelease> { tagMbidRelease }, includeExisting);
            //     }
            // }
            if (idOverrides?.Issue != null)
            {
                // use the release from file tags if it exists and agrees with the specified issue
                if (tagMbidRelease?.Id == idOverrides.Issue.Id)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByBook(idOverrides.Issue, includeExisting);
                }
            }
            else if (idOverrides?.Series != null)
            {
                // use the release from file tags if it exists and agrees with the specified issue
                if (tagMbidRelease?.SeriesMetadataId == idOverrides.Series.SeriesMetadataId)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesBySeries(localEdition, idOverrides.Series, includeExisting);
                }
            }
            else
            {
                if (tagMbidRelease != null)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidates(localEdition, includeExisting);
                }
            }

            watch.Stop();
            _logger.Debug($"Getting {candidateReleases.Count} candidates from tags for {localEdition.LocalBooks.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidatesByIssue(Issue issue, bool includeExisting)
        {
            var existingFiles = includeExisting ? _mediaFileService.GetFilesByBook(issue.Id) : new List<ComicFile>();
            return new List<CandidateEdition>
            {
                new CandidateEdition
                {
                    Issue = issue,
                    ExistingFiles = existingFiles
                }
            };
        }

        private List<CandidateEdition> GetDbCandidatesByBook(Issue issue, bool includeExisting)
        {
            return GetDbCandidatesByIssue(issue, includeExisting);
        }

        private List<CandidateEdition> GetDbCandidatesBySeries(LocalEdition localEdition, Series author, bool includeExisting)
        {
            _logger.Trace("Getting candidates for {0}", author);
            var candidateReleases = new List<CandidateEdition>();

            var bookTag = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";
            if (bookTag.IsNotNullOrWhiteSpace())
            {
                var possibleBooks = _bookService.GetCandidates(author.SeriesMetadataId, bookTag);
                foreach (var issue in possibleBooks)
                {
                    candidateReleases.AddRange(GetDbCandidatesByBook(issue, includeExisting));
                }

                var possibleEditionIssues = _bookService.GetCandidates(author.SeriesMetadataId, bookTag);
                foreach (var possibleIssue in possibleEditionIssues)
                {
                    candidateReleases.AddRange(GetDbCandidatesByIssue(possibleIssue, includeExisting));
                }
            }

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidates(LocalEdition localEdition, bool includeExisting)
        {
            // most general version, nothing has been specified.
            // get all plausible authors, then all plausible issues, then get releases for each of these.
            var candidateReleases = new List<CandidateEdition>();

            // check if it looks like VA.
            if (TrackGroupingService.IsVariousSeriess(localEdition.LocalBooks))
            {
                var va = _authorService.FindById(DistanceCalculator.VariousSeriesIds[0]);
                if (va != null)
                {
                    candidateReleases.AddRange(GetDbCandidatesBySeries(localEdition, va, includeExisting));
                }
            }

            var authorTags = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.Seriess) ?? new List<string>();
            if (authorTags.Any())
            {
                var variants = DistanceCalculator.GetSeriesVariants(authorTags.Where(x => x.IsNotNullOrWhiteSpace()).ToList());

                foreach (var authorTag in variants)
                {
                    if (authorTag.IsNotNullOrWhiteSpace())
                    {
                        var possibleSeriess = _authorService.GetCandidates(authorTag);
                        foreach (var author in possibleSeriess)
                        {
                            candidateReleases.AddRange(GetDbCandidatesBySeries(localEdition, author, includeExisting));
                        }
                    }
                }
            }

            return candidateReleases;
        }

        public IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides)
        {
            // TODO handle edition override

            // Gets candidate issue releases from the metadata server.
            // Will eventually need adding locally if we find a match
            List<Issue> remoteBooks;
            var seenCandidates = new HashSet<string>();

            var isbns = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Isbn).Distinct().ToList();
            var asins = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Asin).Distinct().ToList();
            var goodreads = localEdition.LocalBooks.Select(x => x.FileTrackInfo.GoodreadsId).Distinct().ToList();

            // grab possibilities for all the IDs present
            if (isbns.Count == 1 && isbns[0].IsNotNullOrWhiteSpace())
            {
                _logger.Trace($"Searching by isbn {isbns[0]}");

                try
                {
                    remoteBooks = _bookSearchService.SearchByIsbn(isbns[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ISBN search due to Goodreads Error");
                    remoteBooks = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (asins.Count == 1 &&
                asins[0].IsNotNullOrWhiteSpace() &&
                asins[0].Length == 10)
            {
                _logger.Trace($"Searching by asin {asins[0]}");

                try
                {
                    remoteBooks = _bookSearchService.SearchByAsin(asins[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ASIN search due to Goodreads Error");
                    remoteBooks = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (goodreads.Count == 1 &&
                goodreads[0].IsNotNullOrWhiteSpace())
            {
                if (int.TryParse(goodreads[0], out var id))
                {
                    _logger.Trace($"Searching by goodreads id {id}");

                    try
                    {
                        remoteBooks = _bookSearchService.SearchByGoodreadsBookId(id, true);
                    }
                    catch (GoodreadsException e)
                    {
                        _logger.Info(e, "Skipping Goodreads ID search due to Goodreads Error");
                        remoteBooks = new List<Issue>();
                    }

                    foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                    {
                        yield return candidate;
                    }
                }
            }

            // If we got an id result, or any overrides are set, stop
            if (seenCandidates.Any() ||
                idOverrides?.Issue != null ||
                idOverrides?.Series != null)
            {
                yield break;
            }

            // fall back to author / issue name search
            var authorTags = new List<string>();

            if (TrackGroupingService.IsVariousSeriess(localEdition.LocalBooks))
            {
                authorTags.Add("Various Seriess");
            }
            else
            {
                // the most common list of authors reported by a file
                var authors = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Seriess.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                    .GroupBy(x => x.ConcatToString())
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First();
                authorTags.AddRange(authors);
            }

            var bookTag = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";

            // If no valid author or issue tags, stop
            if (!authorTags.Any() || bookTag.IsNullOrWhiteSpace())
            {
                yield break;
            }

            // Search by author+issue
            foreach (var authorTag in authorTags)
            {
                try
                {
                    remoteBooks = _bookSearchService.SearchForNewBook(bookTag, authorTag);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping author/title search due to Goodreads Error");
                    remoteBooks = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            // If we got an author/issue search result, stop
            if (seenCandidates.Any())
            {
                yield break;
            }

            // Search by just issue title
            try
            {
                remoteBooks = _bookSearchService.SearchForNewBook(bookTag, null);
            }
            catch (GoodreadsException e)
            {
                _logger.Info(e, "Skipping issue title search due to Goodreads Error");
                remoteBooks = new List<Issue>();
            }

            foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
            {
                yield return candidate;
            }

            // Search by just author
            foreach (var a in authorTags)
            {
                try
                {
                    remoteBooks = _bookSearchService.SearchForNewBook(a, null);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping author search due to Goodreads Error");
                    remoteBooks = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }
        }

        private List<CandidateEdition> ToCandidates(IEnumerable<Issue> issues, HashSet<string> seenCandidates, IdentificationOverrides idOverrides)
        {
            var candidates = new List<CandidateEdition>();

            foreach (var issue in issues)
            {
                if (!seenCandidates.Contains(issue.ForeignIssueId) && SatisfiesOverride(issue, idOverrides))
                {
                    seenCandidates.Add(issue.ForeignIssueId);
                    candidates.Add(new CandidateEdition
                    {
                        Issue = issue,
                        ExistingFiles = new List<ComicFile>()
                    });
                }
            }

            return candidates;
        }

        private bool SatisfiesOverride(Issue issue, IdentificationOverrides idOverride)
        {
            if (idOverride?.Issue != null)
            {
                return issue.ForeignIssueId == idOverride.Issue.ForeignIssueId;
            }

            if (idOverride?.Series != null)
            {
                return issue.Series.Value.ForeignSeriesId == idOverride.Series.ForeignSeriesId;
            }

            return true;
        }
    }
}
