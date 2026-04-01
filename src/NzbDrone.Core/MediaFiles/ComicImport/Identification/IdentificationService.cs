using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public interface IIdentificationService
    {
        List<LocalEdition> Identify(List<LocalIssue> localTracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config);
    }

    public class IdentificationService : IIdentificationService
    {
        private readonly ITrackGroupingService _trackGroupingService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IAugmentingService _augmentingService;
        private readonly ICandidateService _candidateService;
        private readonly Logger _logger;

        public IdentificationService(ITrackGroupingService trackGroupingService,
                                     IMetadataTagService metadataTagService,
                                     IAugmentingService augmentingService,
                                     ICandidateService candidateService,
                                     Logger logger)
        {
            _trackGroupingService = trackGroupingService;
            _metadataTagService = metadataTagService;
            _augmentingService = augmentingService;
            _candidateService = candidateService;
            _logger = logger;
        }

        public List<LocalEdition> GetLocalIssueReleases(List<LocalIssue> localTracks, bool singleRelease)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            List<LocalEdition> releases;
            if (singleRelease)
            {
                releases = new List<LocalEdition> { new LocalEdition(localTracks) };
            }
            else
            {
                releases = _trackGroupingService.GroupTracks(localTracks);
            }

            _logger.Debug($"Sorted {localTracks.Count} tracks into {releases.Count} releases in {watch.ElapsedMilliseconds}ms");

            foreach (var localRelease in releases)
            {
                try
                {
                    _augmentingService.Augment(localRelease);
                }
                catch (AugmentingFailedException)
                {
                    _logger.Warn($"Augmentation failed for {localRelease}");
                }
            }

            return releases;
        }

        public List<LocalEdition> Identify(List<LocalIssue> localTracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config)
        {
            // 1 group localTracks so that we think they represent a single release
            // 2 get candidates given specified series, issue and release.  Candidates can include extra files already on disk.
            // 3 find best candidate
            var watch = System.Diagnostics.Stopwatch.StartNew();

            _logger.Debug("Starting issue identification");

            var releases = GetLocalIssueReleases(localTracks, config.SingleRelease);

            var i = 0;
            foreach (var localRelease in releases)
            {
                i++;
                _logger.ProgressInfo($"Identifying issue {i}/{releases.Count}");
                _logger.Debug($"Identifying issue files:\n{localRelease.LocalIssues.Select(x => x.Path).ConcatToString("\n")}");

                try
                {
                    IdentifyRelease(localRelease, idOverrides, config);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error identifying release");
                }
            }

            watch.Stop();

            _logger.Debug($"Track identification for {localTracks.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return releases;
        }

        private List<LocalIssue> ToLocalTrack(IEnumerable<ComicFile> trackfiles, LocalEdition localRelease)
        {
            var scanned = trackfiles.Join(localRelease.LocalIssues, t => t.Path, l => l.Path, (track, localTrack) => localTrack);
            var toScan = trackfiles.ExceptBy(t => t.Path, scanned, s => s.Path, StringComparer.InvariantCulture);
            var localTracks = scanned.Concat(toScan.Select(x => new LocalIssue
            {
                Path = x.Path,
                Size = x.Size,
                Modified = x.Modified,
                FileTrackInfo = _metadataTagService.ReadTags((FileInfoBase)new FileInfo(x.Path)),
                ExistingFile = true,
                AdditionalFile = true,
                Quality = x.Quality
            }))
            .ToList();

            localTracks.ForEach(x => _augmentingService.Augment(x, true));

            return localTracks;
        }

        private void IdentifyRelease(LocalEdition localIssueRelease, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var usedRemote = false;

            IEnumerable<CandidateEdition> candidateReleases = _candidateService.GetDbCandidatesFromTags(localIssueRelease, idOverrides, config.IncludeExisting);

            // convert all the TrackFiles that represent extra files to List<LocalTrack>
            // local candidates are actually a list so this is fine to enumerate
            var allLocalTracks = ToLocalTrack(candidateReleases
                .SelectMany(x => x.ExistingFiles)
                .DistinctBy(x => x.Path), localIssueRelease);

            _logger.Debug($"Retrieved {allLocalTracks.Count} possible tracks in {watch.ElapsedMilliseconds}ms");

            if (!candidateReleases.Any())
            {
                _logger.Debug("No local candidates found, trying remote");
                candidateReleases = _candidateService.GetRemoteCandidates(localIssueRelease, idOverrides);
                if (!config.AddNewSeries)
                {
                    candidateReleases = candidateReleases.Where(x => x.Issue.Id > 0 && x.Issue.SeriesId > 0);
                }

                usedRemote = true;
            }

            GetBestRelease(localIssueRelease, candidateReleases, allLocalTracks, out var seenCandidate);

            // If candidates were found but distance calc failed (e.g. no embedded metadata),
            // and we have a series override, force-accept the best candidate.
            // This handles direct-downloaded comics without ComicInfo.xml.
            if (seenCandidate && localIssueRelease.Issue == null && idOverrides?.Series != null)
            {
                var candidateList = _candidateService.GetDbCandidatesFromTags(localIssueRelease, idOverrides, config.IncludeExisting);
                if (candidateList.Count == 1)
                {
                    var forcedIssue = candidateList[0].Issue;
                    _logger.Debug("Distance calc failed but single candidate found via series override, force-accepting: {0}", forcedIssue.Title);
                    localIssueRelease.Issue = forcedIssue;
                    localIssueRelease.Distance = new Distance();
                    localIssueRelease.ExistingTracks = new List<LocalIssue>();
                    foreach (var localTrack in localIssueRelease.LocalIssues)
                    {
                        localTrack.Issue = forcedIssue;
                        localTrack.Series = idOverrides.Series;
                    }

                    return;
                }
            }

            if (!seenCandidate)
            {
                // can't find any candidates even after using remote search
                // populate the overrides and return
                foreach (var localTrack in localIssueRelease.LocalIssues)
                {
                    localTrack.Issue = idOverrides.Issue;
                    localTrack.Series = idOverrides.Series;
                }

                return;
            }

            // If the result isn't great and we haven't tried remote candidates, try looking for remote candidates
            // The metadata provider may have a better edition of a local issue
            if (localIssueRelease.Distance.NormalizedDistance() > 0.15 && !usedRemote)
            {
                _logger.Debug("Match not good enough, trying remote candidates");
                candidateReleases = _candidateService.GetRemoteCandidates(localIssueRelease, idOverrides);

                if (!config.AddNewSeries)
                {
                    candidateReleases = candidateReleases.Where(x => x.Issue.Id > 0);
                }

                GetBestRelease(localIssueRelease, candidateReleases, allLocalTracks, out _);
            }

            _logger.Debug($"Best release found in {watch.ElapsedMilliseconds}ms");

            localIssueRelease.PopulateMatch(config.KeepAllEditions);

            _logger.Debug($"IdentifyRelease done in {watch.ElapsedMilliseconds}ms");
        }

        private void GetBestRelease(LocalEdition localIssueRelease, IEnumerable<CandidateEdition> candidateReleases, List<LocalIssue> extraTracksOnDisk, out bool seenCandidate)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            _logger.Debug("Matching {0} track files against candidates", localIssueRelease.TrackCount);
            _logger.Trace("Processing files:\n{0}", string.Join("\n", localIssueRelease.LocalIssues.Select(x => x.Path)));

            var bestDistance = localIssueRelease.Issue != null ? localIssueRelease.Distance.NormalizedDistance() : 1.0;
            seenCandidate = false;

            foreach (var candidateRelease in candidateReleases)
            {
                seenCandidate = true;

                var release = candidateRelease.Issue;
                _logger.Debug($"Trying Release {release}");
                var rwatch = System.Diagnostics.Stopwatch.StartNew();

                var extraTrackPaths = candidateRelease.ExistingFiles.Select(x => x.Path).ToList();
                var extraTracks = extraTracksOnDisk.Where(x => extraTrackPaths.Contains(x.Path)).ToList();
                var allLocalTracks = localIssueRelease.LocalIssues.Concat(extraTracks).DistinctBy(x => x.Path).ToList();

                var distance = DistanceCalculator.IssueDistance(allLocalTracks, release);
                var currDistance = distance.NormalizedDistance();

                rwatch.Stop();
                _logger.Debug("Release {0} has distance {1} vs best distance {2} [{3}ms]",
                              release,
                              currDistance,
                              bestDistance,
                              rwatch.ElapsedMilliseconds);
                if (currDistance < bestDistance)
                {
                    bestDistance = currDistance;
                    localIssueRelease.Distance = distance;
                    localIssueRelease.Issue = release;
                    localIssueRelease.ExistingTracks = extraTracks;
                    if (currDistance == 0.0)
                    {
                        break;
                    }
                }
            }

            watch.Stop();
            _logger.Debug($"Best release: {localIssueRelease.Issue} Distance {localIssueRelease.Distance.NormalizedDistance()} found in {watch.ElapsedMilliseconds}ms");
        }
    }
}
