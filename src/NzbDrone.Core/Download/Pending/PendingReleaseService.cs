using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Crypto;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Download.Pending
{
    public interface IPendingReleaseService
    {
        void Add(DownloadDecision decision, PendingReleaseReason reason);
        void AddMany(List<Tuple<DownloadDecision, PendingReleaseReason>> decisions);
        List<ReleaseInfo> GetPending();
        List<RemoteIssue> GetPendingRemoteIssues(int seriesId);
        List<Queue.Queue> GetPendingQueue();
        Queue.Queue FindPendingQueueItem(int queueId);
        void RemovePendingQueueItems(int queueId);
        RemoteIssue OldestPendingRelease(int seriesId, int[] issueIds);
    }

    public class PendingReleaseService : IPendingReleaseService,
                                         IHandle<SeriesDeletedEvent>,
                                         IHandle<IssueGrabbedEvent>,
                                         IHandle<RssSyncCompleteEvent>
    {
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IPendingReleaseRepository _repository;
        private readonly ISeriesService _seriesService;
        private readonly IParsingService _parsingService;
        private readonly IDelayProfileService _delayProfileService;
        private readonly ITaskManager _taskManager;
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IRemoteIssueAggregationService _aggregationService;
        private readonly IDownloadClientFactory _downloadClientFactory;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public PendingReleaseService(IIndexerStatusService indexerStatusService,
                                    IPendingReleaseRepository repository,
                                    ISeriesService seriesService,
                                    IParsingService parsingService,
                                    IDelayProfileService delayProfileService,
                                    ITaskManager taskManager,
                                    IConfigService configService,
                                    ICustomFormatCalculationService formatCalculator,
                                    IRemoteIssueAggregationService aggregationService,
                                    IDownloadClientFactory downloadClientFactory,
                                    IIndexerFactory indexerFactory,
                                    IEventAggregator eventAggregator,
                                    Logger logger)
        {
            _indexerStatusService = indexerStatusService;
            _repository = repository;
            _seriesService = seriesService;
            _parsingService = parsingService;
            _delayProfileService = delayProfileService;
            _taskManager = taskManager;
            _configService = configService;
            _formatCalculator = formatCalculator;
            _aggregationService = aggregationService;
            _downloadClientFactory = downloadClientFactory;
            _indexerFactory = indexerFactory;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Add(DownloadDecision decision, PendingReleaseReason reason)
        {
            AddMany(new List<Tuple<DownloadDecision, PendingReleaseReason>> { Tuple.Create(decision, reason) });
        }

        public void AddMany(List<Tuple<DownloadDecision, PendingReleaseReason>> decisions)
        {
            foreach (var seriesDecisions in decisions.GroupBy(v => v.Item1.RemoteIssue.Series.Id))
            {
                var series = seriesDecisions.First().Item1.RemoteIssue.Series;
                var alreadyPending = _repository.AllBySeriesId(series.Id);

                alreadyPending = IncludeRemoteIssues(alreadyPending, seriesDecisions.ToDictionaryIgnoreDuplicates(v => v.Item1.RemoteIssue.Release.Title, v => v.Item1.RemoteIssue));
                var alreadyPendingByIssue = CreateIssueLookup(alreadyPending);

                foreach (var pair in seriesDecisions)
                {
                    var decision = pair.Item1;
                    var reason = pair.Item2;

                    var issueIds = decision.RemoteIssue.Issues.Select(e => e.Id);

                    var existingReports = issueIds.SelectMany(v => alreadyPendingByIssue[v])
                                                    .Distinct().ToList();

                    var matchingReports = existingReports.Where(MatchingReleasePredicate(decision.RemoteIssue.Release)).ToList();

                    if (matchingReports.Any())
                    {
                        var matchingReport = matchingReports.First();

                        if (matchingReport.Reason != reason)
                        {
                            if (matchingReport.Reason == PendingReleaseReason.DownloadClientUnavailable)
                            {
                                _logger.Debug("The release {0} is already pending with reason {1}, not changing reason", decision.RemoteIssue, matchingReport.Reason);
                            }
                            else
                            {
                                _logger.Debug("The release {0} is already pending with reason {1}, changing to {2}", decision.RemoteIssue, matchingReport.Reason, reason);
                                matchingReport.Reason = reason;
                                _repository.Update(matchingReport);
                            }
                        }
                        else
                        {
                            _logger.Debug("The release {0} is already pending with reason {1}, not adding again", decision.RemoteIssue, reason);
                        }

                        if (matchingReports.Count() > 1)
                        {
                            _logger.Debug("The release {0} had {1} duplicate pending, removing duplicates.", decision.RemoteIssue, matchingReports.Count() - 1);

                            foreach (var duplicate in matchingReports.Skip(1))
                            {
                                _repository.Delete(duplicate.Id);
                                alreadyPending.Remove(duplicate);
                                alreadyPendingByIssue = CreateIssueLookup(alreadyPending);
                            }
                        }

                        continue;
                    }

                    _logger.Debug("Adding release {0} to pending releases with reason {1}", decision.RemoteIssue, reason);
                    Insert(decision, reason);
                }
            }
        }

        public List<ReleaseInfo> GetPending()
        {
            var releases = _repository.All().Select(p =>
            {
                var release = p.Release;

                release.PendingReleaseReason = p.Reason;

                return release;
            }).ToList();

            if (releases.Any())
            {
                releases = FilterBlockedIndexers(releases);
            }

            return releases;
        }

        public List<RemoteIssue> GetPendingRemoteIssues(int seriesId)
        {
            return IncludeRemoteIssues(_repository.AllBySeriesId(seriesId)).Select(v => v.RemoteIssue).ToList();
        }

        public List<Queue.Queue> GetPendingQueue()
        {
            var queued = new List<Queue.Queue>();

            var nextRssSync = new Lazy<DateTime>(() => _taskManager.GetNextExecution(typeof(RssSyncCommand)));

            var pendingReleases = IncludeRemoteIssues(_repository.WithoutFallback());
            foreach (var pendingRelease in pendingReleases)
            {
                foreach (var issue in pendingRelease.RemoteIssue.Issues)
                {
                    var ect = pendingRelease.Release.PublishDate.AddMinutes(GetDelay(pendingRelease.RemoteIssue));

                    if (ect < nextRssSync.Value)
                    {
                        ect = nextRssSync.Value;
                    }
                    else
                    {
                        ect = ect.AddMinutes(_configService.RssSyncInterval);
                    }

                    var timeleft = ect.Subtract(DateTime.UtcNow);

                    if (timeleft.TotalSeconds < 0)
                    {
                        timeleft = TimeSpan.Zero;
                    }

                    string downloadClientName = null;
                    var indexer = _indexerFactory.Find(pendingRelease.Release.IndexerId);

                    if (indexer is { DownloadClientId: > 0 })
                    {
                        var downloadClient = _downloadClientFactory.Find(indexer.DownloadClientId);

                        downloadClientName = downloadClient?.Name;
                    }

                    var queue = new Queue.Queue
                    {
                        Id = GetQueueId(pendingRelease, issue),
                        Series = pendingRelease.RemoteIssue.Series,
                        Issue = issue,
                        Quality = pendingRelease.RemoteIssue.ParsedIssueInfo.Quality,
                        Title = pendingRelease.Title,
                        Size = pendingRelease.RemoteIssue.Release.Size,
                        Sizeleft = pendingRelease.RemoteIssue.Release.Size,
                        RemoteIssue = pendingRelease.RemoteIssue,
                        Timeleft = timeleft,
                        EstimatedCompletionTime = ect,
                        Status = pendingRelease.Reason.ToString(),
                        Protocol = pendingRelease.RemoteIssue.Release.DownloadProtocol,
                        Indexer = pendingRelease.RemoteIssue.Release.Indexer,
                        DownloadClient = downloadClientName
                    };

                    queued.Add(queue);
                }
            }

            //Return best quality release for each issue
            var deduped = queued.GroupBy(q => q.Issue.Id).Select(g =>
            {
                var series = g.First().Series;

                return g.OrderByDescending(e => e.Quality, new QualityModelComparer(series.QualityProfile))
                        .ThenBy(q => PrioritizeDownloadProtocol(q.Series, q.Protocol))
                        .First();
            });

            return deduped.ToList();
        }

        public Queue.Queue FindPendingQueueItem(int queueId)
        {
            return GetPendingQueue().SingleOrDefault(p => p.Id == queueId);
        }

        public void RemovePendingQueueItems(int queueId)
        {
            var targetItem = FindPendingRelease(queueId);
            var seriesReleases = _repository.AllBySeriesId(targetItem.SeriesId);

            var releasesToRemove = seriesReleases.Where(
                c => c.ParsedIssueInfo.IssueTitle == targetItem.ParsedIssueInfo.IssueTitle);

            _repository.DeleteMany(releasesToRemove.Select(c => c.Id));
        }

        public RemoteIssue OldestPendingRelease(int seriesId, int[] issueIds)
        {
            var seriesReleases = GetPendingReleases(seriesId);

            return seriesReleases.Select(r => r.RemoteIssue)
                                 .Where(r => r.Issues.Select(e => e.Id).Intersect(issueIds).Any())
                                 .MaxBy(p => p.Release.AgeHours);
        }

        private ILookup<int, PendingRelease> CreateIssueLookup(IEnumerable<PendingRelease> alreadyPending)
        {
            return alreadyPending.SelectMany(v => v.RemoteIssue.Issues
                                                   .Select(d => new { Issue = d, PendingRelease = v }))
                                 .ToLookup(v => v.Issue.Id, v => v.PendingRelease);
        }

        private List<ReleaseInfo> FilterBlockedIndexers(List<ReleaseInfo> releases)
        {
            var blockedIndexers = new HashSet<int>(_indexerStatusService.GetBlockedProviders().Select(v => v.ProviderId));

            return releases.Where(release => !blockedIndexers.Contains(release.IndexerId)).ToList();
        }

        private List<PendingRelease> GetPendingReleases()
        {
            return IncludeRemoteIssues(_repository.All().ToList());
        }

        private List<PendingRelease> GetPendingReleases(int seriesId)
        {
            return IncludeRemoteIssues(_repository.AllBySeriesId(seriesId).ToList());
        }

        private List<PendingRelease> IncludeRemoteIssues(List<PendingRelease> releases, Dictionary<string, RemoteIssue> knownRemoteIssues = null)
        {
            var result = new List<PendingRelease>();

            var seriesMap = new Dictionary<int, Series>();

            if (knownRemoteIssues != null)
            {
                foreach (var series in knownRemoteIssues.Values.Select(v => v.Series))
                {
                    if (!seriesMap.ContainsKey(series.Id))
                    {
                        seriesMap[series.Id] = series;
                    }
                }
            }

            foreach (var series in _seriesService.GetSeries(releases.Select(v => v.SeriesId).Distinct().Where(v => !seriesMap.ContainsKey(v))))
            {
                seriesMap[series.Id] = series;
            }

            foreach (var release in releases)
            {
                var series = seriesMap.GetValueOrDefault(release.SeriesId);

                // Just in case the series was removed, but wasn't cleaned up yet (housekeeper will clean it up)
                if (series == null)
                {
                    return null;
                }

                List<Issue> issues;

                if (knownRemoteIssues != null && knownRemoteIssues.TryGetValue(release.Release.Title, out var knownRemoteIssue))
                {
                    issues = knownRemoteIssue.Issues;
                }
                else
                {
                    issues = _parsingService.GetIssues(release.ParsedIssueInfo, series);
                }

                release.RemoteIssue = new RemoteIssue
                {
                    Series = series,
                    Issues = issues,
                    ReleaseSource = release.AdditionalInfo?.ReleaseSource ?? ReleaseSourceType.Unknown,
                    ParsedIssueInfo = release.ParsedIssueInfo,
                    Release = release.Release
                };

                _aggregationService.Augment(release.RemoteIssue);
                release.RemoteIssue.CustomFormats = _formatCalculator.ParseCustomFormat(release.RemoteIssue, release.Release.Size);

                result.Add(release);
            }

            return result;
        }

        private void Insert(DownloadDecision decision, PendingReleaseReason reason)
        {
            _repository.Insert(new PendingRelease
            {
                SeriesId = decision.RemoteIssue.Series.Id,
                ParsedIssueInfo = decision.RemoteIssue.ParsedIssueInfo,
                Release = decision.RemoteIssue.Release,
                Title = decision.RemoteIssue.Release.Title,
                Added = DateTime.UtcNow,
                Reason = reason,
                AdditionalInfo = new PendingReleaseAdditionalInfo
                {
                    ReleaseSource = decision.RemoteIssue.ReleaseSource
                }
            });

            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private void Delete(PendingRelease pendingRelease)
        {
            _repository.Delete(pendingRelease);
            _eventAggregator.PublishEvent(new PendingReleasesUpdatedEvent());
        }

        private int GetDelay(RemoteIssue remoteIssue)
        {
            var delayProfile = _delayProfileService.AllForTags(remoteIssue.Series.Tags).OrderBy(d => d.Order).First();
            var delay = delayProfile.GetProtocolDelay(remoteIssue.Release.DownloadProtocol);
            var minimumAge = _configService.MinimumAge;

            return new[] { delay, minimumAge }.Max();
        }

        private void RemoveGrabbed(RemoteIssue remoteIssue)
        {
            var pendingReleases = GetPendingReleases(remoteIssue.Series.Id);
            var issueIds = remoteIssue.Issues.Select(e => e.Id);

            var existingReports = pendingReleases.Where(r => r.RemoteIssue.Issues.Select(e => e.Id)
                                                             .Intersect(issueIds)
                                                             .Any())
                                                             .ToList();

            if (existingReports.Empty())
            {
                return;
            }

            var profile = remoteIssue.Series.QualityProfile.Value;

            foreach (var existingReport in existingReports)
            {
                var compare = new QualityModelComparer(profile).Compare(remoteIssue.ParsedIssueInfo.Quality,
                                                                        existingReport.RemoteIssue.ParsedIssueInfo.Quality);

                //Only remove lower/equal quality pending releases
                //It is safer to retry these releases on the next round than remove it and try to re-add it (if its still in the feed)
                if (compare >= 0)
                {
                    _logger.Debug("Removing previously pending release, as it was grabbed.");
                    Delete(existingReport);
                }
            }
        }

        private void RemoveRejected(List<DownloadDecision> rejected)
        {
            _logger.Debug("Removing failed releases from pending");
            var pending = GetPendingReleases();

            foreach (var rejectedRelease in rejected)
            {
                var matching = pending.Where(MatchingReleasePredicate(rejectedRelease.RemoteIssue.Release));

                foreach (var pendingRelease in matching)
                {
                    _logger.Debug("Removing previously pending release, as it has now been rejected.");
                    Delete(pendingRelease);
                }
            }
        }

        private PendingRelease FindPendingRelease(int queueId)
        {
            return GetPendingReleases().First(p => p.RemoteIssue.Issues.Any(e => queueId == GetQueueId(p, e)));
        }

        private int GetQueueId(PendingRelease pendingRelease, Issue issue)
        {
            return HashConverter.GetHashInt31(string.Format("pending-{0}-issue{1}", pendingRelease.Id, issue.Id));
        }

        private int PrioritizeDownloadProtocol(Series series, DownloadProtocol downloadProtocol)
        {
            var delayProfile = _delayProfileService.BestForTags(series.Tags);

            if (downloadProtocol == delayProfile.PreferredProtocol)
            {
                return 0;
            }

            return 1;
        }

        public void Handle(SeriesDeletedEvent message)
        {
            _repository.DeleteBySeriesId(message.Series.Id);
        }

        public void Handle(IssueGrabbedEvent message)
        {
            RemoveGrabbed(message.Issue);
        }

        public void Handle(RssSyncCompleteEvent message)
        {
            RemoveRejected(message.ProcessedDecisions.Rejected);
        }

        private static Func<PendingRelease, bool> MatchingReleasePredicate(ReleaseInfo release)
        {
            return p => p.Title == release.Title &&
                        p.Release.PublishDate == release.PublishDate &&
                        p.Release.Indexer == release.Indexer;
        }
    }
}
