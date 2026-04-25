using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Issues
{
    public interface IIssueService
    {
        Issue GetIssue(int issueId);
        List<Issue> GetIssues(IEnumerable<int> issueIds);
        List<Issue> GetIssuesBySeries(int seriesId);
        List<Issue> GetNextIssuesBySeriesMetadataId(IEnumerable<int> seriesMetadataIds);
        List<Issue> GetLastIssuesBySeriesMetadataId(IEnumerable<int> seriesMetadataIds);
        List<Issue> GetIssuesBySeriesMetadataId(int seriesMetadataId);
        List<Issue> GetIssuesForRefresh(int seriesMetadataId, List<string> foreignIds);
        List<Issue> GetIssuesByFileIds(IEnumerable<int> fileIds);
        Issue AddIssue(Issue newIssue, bool doRefresh = true);
        Issue FindById(string foreignId);
        Issue FindBySlug(string titleSlug);
        Issue FindByTitle(int seriesMetadataId, string title);
        Issue FindByTitleInexact(int seriesMetadataId, string title);
        List<Issue> GetCandidates(int seriesMetadataId, string title);
        void DeleteIssue(int issueId, bool deleteFiles, bool addImportListExclusion = false);
        List<Issue> GetAllIssues();
        Issue UpdateIssue(Issue issue);
        void SetIssueMonitored(int issueId, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        void UpdateLastSearchTime(List<Issue> issues);
        PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec);
        List<Issue> IssuesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored);
        List<Issue> SeriesIssuesBetweenDates(Series series, DateTime start, DateTime end, bool includeUnmonitored);
        void InsertMany(List<Issue> issues);
        void UpdateMany(List<Issue> issues);
        void DeleteMany(List<Issue> issues);
        void SetAddOptions(IEnumerable<Issue> issues);
        List<Issue> GetSeriesIssuesWithFiles(Series series);
    }

    public class IssueService : IIssueService,
                                IHandle<SeriesDeletedEvent>
    {
        private readonly IIssueRepository _issueRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public IssueService(IIssueRepository issueRepository,
                           IEventAggregator eventAggregator,
                           Logger logger)
        {
            _issueRepository = issueRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Issue AddIssue(Issue newIssue, bool doRefresh = true)
        {
            if (newIssue.SeriesMetadataId == 0)
            {
                throw new InvalidOperationException("Cannot insert issue with SeriesMetadataId = 0");
            }

            _issueRepository.Upsert(newIssue);

            _eventAggregator.PublishEvent(new IssueAddedEvent(GetIssue(newIssue.Id), doRefresh));

            return newIssue;
        }

        public void DeleteIssue(int issueId, bool deleteFiles, bool addImportListExclusion = false)
        {
            var issue = _issueRepository.Get(issueId);
            issue.Series.LazyLoad();
            _issueRepository.Delete(issueId);
            _eventAggregator.PublishEvent(new IssueDeletedEvent(issue, deleteFiles, addImportListExclusion));
        }

        public Issue FindById(string foreignId)
        {
            return _issueRepository.FindById(foreignId);
        }

        public Issue FindBySlug(string titleSlug)
        {
            return _issueRepository.FindBySlug(titleSlug);
        }

        public Issue FindByTitle(int seriesMetadataId, string title)
        {
            return _issueRepository.FindByTitle(seriesMetadataId, title);
        }

        private List<Tuple<Func<Issue, string, double>, string>> IssueScoringFunctions(string title, string cleanTitle)
        {
            Func<Func<Issue, string, double>, string, Tuple<Func<Issue, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Issue, string, double>, string>>
            {
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), cleanTitle),
                tc((a, t) => a.Title.FuzzyMatch(t), title),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveBracketsAndContents().CleanSeriesName()),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveAfterDash().CleanSeriesName()),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveBracketsAndContents().RemoveAfterDash().CleanSeriesName()),
                tc((a, t) => t.FuzzyContains(a.CleanTitle), cleanTitle),
                tc((a, t) => t.FuzzyContains(a.Title), title),
                tc((a, t) => a.Title.SplitIssueTitle(a.SeriesMetadata.Value.Name).Item1.FuzzyMatch(t), title)
            };

            return scoringFunctions;
        }

        public Issue FindByTitleInexact(int seriesMetadataId, string title)
        {
            var issues = GetIssuesBySeriesMetadataId(seriesMetadataId);

            foreach (var func in IssueScoringFunctions(title, title.CleanSeriesName()))
            {
                var results = FindByStringInexact(issues, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Issue> GetCandidates(int seriesMetadataId, string title)
        {
            var issues = GetIssuesBySeriesMetadataId(seriesMetadataId);
            var output = new List<Issue>();

            foreach (var func in IssueScoringFunctions(title, title.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(issues, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        private List<Issue> FindByStringInexact(List<Issue> issues, Func<Issue, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.7;
            const double fuzzGap = 0.4;

            var sortedIssues = issues.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Issue = s
            })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedIssues.TakeWhile((x, i) => i == 0 || sortedIssues[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedIssues[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Issue)
                .ToList();
        }

        public List<Issue> GetAllIssues()
        {
            return _issueRepository.All().ToList();
        }

        public Issue GetIssue(int issueId)
        {
            return _issueRepository.Get(issueId);
        }

        public List<Issue> GetIssues(IEnumerable<int> issueIds)
        {
            return _issueRepository.Get(issueIds).ToList();
        }

        public List<Issue> GetIssuesBySeries(int seriesId)
        {
            return _issueRepository.GetIssues(seriesId).ToList();
        }

        public List<Issue> GetNextIssuesBySeriesMetadataId(IEnumerable<int> seriesMetadataIds)
        {
            return _issueRepository.GetNextIssues(seriesMetadataIds).ToList();
        }

        public List<Issue> GetLastIssuesBySeriesMetadataId(IEnumerable<int> seriesMetadataIds)
        {
            return _issueRepository.GetLastIssues(seriesMetadataIds).ToList();
        }

        public List<Issue> GetIssuesBySeriesMetadataId(int seriesMetadataId)
        {
            return _issueRepository.GetIssuesBySeriesMetadataId(seriesMetadataId).ToList();
        }

        public List<Issue> GetIssuesForRefresh(int seriesMetadataId, List<string> foreignIds)
        {
            return _issueRepository.GetIssuesForRefresh(seriesMetadataId, foreignIds);
        }

        public List<Issue> GetIssuesByFileIds(IEnumerable<int> fileIds)
        {
            return _issueRepository.GetIssuesByFileIds(fileIds);
        }

        public void SetAddOptions(IEnumerable<Issue> issues)
        {
            _issueRepository.SetFields(issues.ToList(), s => s.AddOptions);
        }

        public PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec)
        {
            var issueResult = _issueRepository.IssuesWithoutFiles(pagingSpec);

            return issueResult;
        }

        public List<Issue> IssuesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored)
        {
            var issues = _issueRepository.IssuesBetweenDates(start.ToUniversalTime(), end.ToUniversalTime(), includeUnmonitored);

            return issues;
        }

        public List<Issue> SeriesIssuesBetweenDates(Series series, DateTime start, DateTime end, bool includeUnmonitored)
        {
            var issues = _issueRepository.SeriesIssuesBetweenDates(series, start.ToUniversalTime(), end.ToUniversalTime(), includeUnmonitored);

            return issues;
        }

        public List<Issue> GetSeriesIssuesWithFiles(Series series)
        {
            return _issueRepository.GetSeriesIssuesWithFiles(series);
        }

        public void InsertMany(List<Issue> issues)
        {
            if (issues.Any(x => x.SeriesMetadataId == 0))
            {
                throw new InvalidOperationException("Cannot insert issue with SeriesMetadataId = 0");
            }

            _issueRepository.InsertMany(issues);
        }

        public void UpdateMany(List<Issue> issues)
        {
            _issueRepository.UpdateMany(issues);
        }

        public void DeleteMany(List<Issue> issues)
        {
            _issueRepository.DeleteMany(issues);

            foreach (var issue in issues)
            {
                _eventAggregator.PublishEvent(new IssueDeletedEvent(issue, false, false));
            }
        }

        public Issue UpdateIssue(Issue issue)
        {
            var storedIssue = GetIssue(issue.Id);
            var updatedIssue = _issueRepository.Update(issue);

            _eventAggregator.PublishEvent(new IssueEditedEvent(updatedIssue, storedIssue));

            return updatedIssue;
        }

        public void SetIssueMonitored(int issueId, bool monitored)
        {
            var issue = _issueRepository.Get(issueId);
            _issueRepository.SetMonitoredFlat(issue, monitored);

            // publish issue edited event so series stats update
            _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));

            _logger.Debug("Monitored flag for Issue:{0} was set to {1}", issueId, monitored);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            _issueRepository.SetMonitored(ids, monitored);

            // publish issue edited event so series stats update
            foreach (var issue in _issueRepository.Get(ids))
            {
                _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));
            }
        }

        public void UpdateLastSearchTime(List<Issue> issues)
        {
            _issueRepository.SetFields(issues, b => b.LastSearchTime);
        }

        public void Handle(SeriesDeletedEvent message)
        {
            var issues = GetIssuesBySeriesMetadataId(message.Series.SeriesMetadataId);
            DeleteMany(issues);
        }
    }
}
