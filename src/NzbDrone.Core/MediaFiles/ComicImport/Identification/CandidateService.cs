using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
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
        private readonly ISearchForNewIssue _issueSearchService;
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public CandidateService(ISearchForNewIssue issueSearchService,
                                ISeriesService seriesService,
                                IIssueService issueService,
                                IMediaFileService mediaFileService,
                                Logger logger)
        {
            _issueSearchService = issueSearchService;
            _seriesService = seriesService;
            _issueService = issueService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Generally series, issue and release are null.  But if they're not then limit candidates appropriately.
            // We've tried to make sure that tracks are all for a single release.
            List<CandidateEdition> candidateReleases;

            // if we have a Issue ID, use that
            Issue tagMbidRelease = null;
            List<CandidateEdition> tagCandidate = null;

            if (idOverrides?.Issue != null)
            {
                // use the release from file tags if it exists and agrees with the specified issue
                if (tagMbidRelease?.Id == idOverrides.Issue.Id)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByIssue(idOverrides.Issue, includeExisting);
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
            _logger.Debug($"Getting {candidateReleases.Count} candidates from tags for {localEdition.LocalIssues.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidatesByIssue(Issue issue, bool includeExisting)
        {
            var existingFiles = includeExisting ? _mediaFileService.GetFilesByIssue(issue.Id) : new List<ComicFile>();
            return new List<CandidateEdition>
            {
                new CandidateEdition
                {
                    Issue = issue,
                    ExistingFiles = existingFiles
                }
            };
        }

        private List<CandidateEdition> GetDbCandidatesBySeries(LocalEdition localEdition, Series series, bool includeExisting)
        {
            _logger.Trace("Getting candidates for {0}", series);
            var candidateReleases = new List<CandidateEdition>();

            // If we have an explicit issue number from embedded ComicInfo, use it first — most reliable
            var issueIndexTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.SeriesIndex) ?? "";
            if (issueIndexTag.IsNotNullOrWhiteSpace()
                && float.TryParse(issueIndexTag, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var explicitIssueNumber))
            {
                _logger.Debug("Matching series {0} by explicit issue number {1} from embedded metadata", series.SeriesMetadataId, explicitIssueNumber);
                var allIssues = _issueService.GetIssuesBySeriesMetadataId(series.SeriesMetadataId);
                var matchByNumber = allIssues.Where(x => x.IssueNumber == explicitIssueNumber).ToList();
                foreach (var issue in matchByNumber)
                {
                    candidateReleases.AddRange(GetDbCandidatesByIssue(issue, includeExisting));
                }

                if (candidateReleases.Any())
                {
                    return candidateReleases;
                }
            }

            // Try embedded metadata first, then download client title, then filename
            var issueTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";

            if (issueTag.IsNullOrWhiteSpace())
            {
                issueTag = localEdition.LocalIssues.MostCommon(x => x.DownloadClientIssueInfo?.IssueTitle) ?? "";
            }

            if (issueTag.IsNullOrWhiteSpace())
            {
                issueTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.CleanTitle) ?? "";
            }

            if (issueTag.IsNullOrWhiteSpace())
            {
                issueTag = localEdition.LocalIssues
                    .Select(x => System.IO.Path.GetFileNameWithoutExtension(x.Path))
                    .FirstOrDefault() ?? "";
            }

            if (issueTag.IsNotNullOrWhiteSpace())
            {
                var possibleIssues = _issueService.GetCandidates(series.SeriesMetadataId, issueTag);
                foreach (var issue in possibleIssues)
                {
                    candidateReleases.AddRange(GetDbCandidatesByIssue(issue, includeExisting));
                }
            }

            // If title matching found nothing, try matching by issue number
            if (!candidateReleases.Any())
            {
                var issueNumberStr = System.Text.RegularExpressions.Regex.Match(issueTag, @"#?(\d+\.?\d*)").Groups[1].Value;
                if (float.TryParse(issueNumberStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var issueNumber))
                {
                    _logger.Debug("Title match failed for series {0}, trying issue number {1}", series.SeriesMetadataId, issueNumber);
                    var allIssues = _issueService.GetIssuesBySeriesMetadataId(series.SeriesMetadataId);
                    var matchByNumber = allIssues.Where(x => x.IssueNumber == issueNumber).ToList();
                    foreach (var issue in matchByNumber)
                    {
                        candidateReleases.AddRange(GetDbCandidatesByIssue(issue, includeExisting));
                    }
                }
            }

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidates(LocalEdition localEdition, bool includeExisting)
        {
            // most general version, nothing has been specified.
            // get all plausible allSeries, then all plausible issues, then get releases for each of these.
            var candidateReleases = new List<CandidateEdition>();

            // check if it looks like VA.
            if (TrackGroupingService.IsVariousSeries(localEdition.LocalIssues))
            {
                var va = _seriesService.FindById(DistanceCalculator.VariousSeriesIds[0]);
                if (va != null)
                {
                    candidateReleases.AddRange(GetDbCandidatesBySeries(localEdition, va, includeExisting));
                }
            }

            var seriesTags = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.Series) ?? new List<string>();
            if (seriesTags.Any())
            {
                var variants = DistanceCalculator.GetSeriesVariants(seriesTags.Where(x => x.IsNotNullOrWhiteSpace()).ToList());

                foreach (var seriesTag in variants)
                {
                    if (seriesTag.IsNotNullOrWhiteSpace())
                    {
                        var possibleSeries = _seriesService.GetCandidates(seriesTag);
                        foreach (var series in possibleSeries)
                        {
                            candidateReleases.AddRange(GetDbCandidatesBySeries(localEdition, series, includeExisting));
                        }
                    }
                }
            }

            return candidateReleases;
        }

        public IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides)
        {
            // Gets candidate issue releases from the metadata server.
            // Will eventually need adding locally if we find a match
            List<Issue> remoteIssues;
            var seenCandidates = new HashSet<string>();

            // If any overrides are set, stop
            if (idOverrides?.Issue != null ||
                idOverrides?.Series != null)
            {
                yield break;
            }

            // fall back to series / issue name search
            var seriesTags = new List<string>();

            if (TrackGroupingService.IsVariousSeries(localEdition.LocalIssues))
            {
                seriesTags.Add("Various Series");
            }
            else
            {
                // the most common list of allSeries reported by a file
                var allSeries = localEdition.LocalIssues.Select(x => x.FileTrackInfo.Series.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                    .GroupBy(x => x.ConcatToString())
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First();
                seriesTags.AddRange(allSeries);
            }

            var issueTag = localEdition.LocalIssues.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";

            // If no valid series or issue tags, stop
            if (!seriesTags.Any() || issueTag.IsNullOrWhiteSpace())
            {
                yield break;
            }

            // Search by series+issue
            foreach (var seriesTag in seriesTags)
            {
                try
                {
                    remoteIssues = _issueSearchService.SearchForNewIssue(issueTag, seriesTag);
                }
                catch (System.Exception e)
                {
                    _logger.Info(e, "Skipping series/title search due to error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            // If we got an series/issue search result, stop
            if (seenCandidates.Any())
            {
                yield break;
            }

            // Search by just issue title
            try
            {
                remoteIssues = _issueSearchService.SearchForNewIssue(issueTag, null);
            }
            catch (System.Exception e)
            {
                _logger.Info(e, "Skipping issue title search due to error");
                remoteIssues = new List<Issue>();
            }

            foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
            {
                yield return candidate;
            }

            // Search by just series
            foreach (var a in seriesTags)
            {
                try
                {
                    remoteIssues = _issueSearchService.SearchForNewIssue(a, null);
                }
                catch (System.Exception e)
                {
                    _logger.Info(e, "Skipping series search due to error");
                    remoteIssues = new List<Issue>();
                }

                foreach (var candidate in ToCandidates(remoteIssues, seenCandidates, idOverrides))
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
