using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesService
    {
        Series GetSeries(int seriesId);
        Series GetSeriesByMetadataId(int seriesMetadataId);
        List<Series> GetSeries(IEnumerable<int> seriesIds);
        Series AddSeries(Series newSeries, bool doRefresh);
        List<Series> AddSeries(List<Series> newSeriesList, bool doRefresh);
        Series FindById(string foreignSeriesId);
        Series FindByName(string title);
        Series FindByNameInexact(string title);
        List<Series> GetCandidates(string title);
        List<Series> GetReportCandidates(string reportTitle);
        void DeleteSeries(int seriesId, bool deleteFiles, bool addImportListExclusion = false);
        List<Series> GetAllSeries();
        Dictionary<int, List<int>> GetAllSeriesTags();
        List<Series> AllForTag(int tagId);
        Series UpdateSeries(Series series);
        List<Series> UpdateSeries(List<Series> allSeries, bool useExistingRelativeFolder);
        Dictionary<int, string> AllSeriesPaths();
        bool SeriesPathExists(string folder);
        void RemoveAddOptions(Series series);
    }

    public class SeriesService : ISeriesService
    {
        private readonly ISeriesRepository _seriesRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildSeriesPaths _seriesPathBuilder;
        private readonly Logger _logger;
        private readonly ICached<List<Series>> _cache;

        public SeriesService(ISeriesRepository seriesRepository,
                             IEventAggregator eventAggregator,
                             IBuildSeriesPaths seriesPathBuilder,
                             ICacheManager cacheManager,
                             Logger logger)
        {
            _seriesRepository = seriesRepository;
            _eventAggregator = eventAggregator;
            _seriesPathBuilder = seriesPathBuilder;
            _cache = cacheManager.GetRollingCache<List<Series>>(GetType(), "seriescache", TimeSpan.FromSeconds(30));
            _logger = logger;
        }

        public Series AddSeries(Series newSeries, bool doRefresh)
        {
            _cache.Clear();
            _seriesRepository.Insert(newSeries);
            _eventAggregator.PublishEvent(new SeriesAddedEvent(GetSeries(newSeries.Id), doRefresh));

            return newSeries;
        }

        public List<Series> AddSeries(List<Series> newSeriesList, bool doRefresh)
        {
            _cache.Clear();
            _seriesRepository.InsertMany(newSeriesList);
            _eventAggregator.PublishEvent(new SeriesImportedEvent(newSeriesList.Select(s => s.Id).ToList(), doRefresh));

            return newSeriesList;
        }

        public bool SeriesPathExists(string folder)
        {
            return _seriesRepository.SeriesPathExists(folder);
        }

        public void DeleteSeries(int seriesId, bool deleteFiles, bool addImportListExclusion = false)
        {
            _cache.Clear();
            var series = _seriesRepository.Get(seriesId);
            _seriesRepository.Delete(seriesId);
            _eventAggregator.PublishEvent(new SeriesDeletedEvent(series, deleteFiles, addImportListExclusion));
        }

        public Series FindById(string foreignSeriesId)
        {
            return _seriesRepository.FindById(foreignSeriesId);
        }

        public Series FindByName(string title)
        {
            return _seriesRepository.FindByName(title.CleanSeriesName());
        }

        public List<Tuple<Func<Series, string, double>, string>> SeriesScoringFunctions(string title, string cleanTitle)
        {
            Func<Func<Series, string, double>, string, Tuple<Func<Series, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Series, string, double>, string>>
            {
                tc((a, t) => a.Metadata.Value.Name.FuzzyMatch(t), title)
            };

            return scoringFunctions;
        }

        public Series FindByNameInexact(string title)
        {
            var allSeries = GetAllSeries();

            foreach (var func in SeriesScoringFunctions(title, title.CleanSeriesName()))
            {
                var results = FindByStringInexact(allSeries, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Series> GetCandidates(string title)
        {
            var allSeries = GetAllSeries();
            var output = new List<Series>();

            foreach (var func in SeriesScoringFunctions(title, title.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(allSeries, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        public List<Tuple<Func<Series, string, double>, string>> ReportSeriesScoringFunctions(string reportTitle, string cleanReportTitle)
        {
            Func<Func<Series, string, double>, string, Tuple<Func<Series, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Series, string, double>, string>>
            {
                tc((a, t) => t.FuzzyMatch(a.Metadata.Value.Name, 0.6).Item3, reportTitle)
            };

            return scoringFunctions;
        }

        public List<Series> GetReportCandidates(string reportTitle)
        {
            var allSeries = GetAllSeries();
            var output = new List<Series>();

            foreach (var func in ReportSeriesScoringFunctions(reportTitle, reportTitle.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(allSeries, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        private List<Series> FindByStringInexact(List<Series> allSeries, Func<Series, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.8;
            const double fuzzGap = 0.2;

            var sortedSeries = allSeries.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Series = s
            })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedSeries.TakeWhile((x, i) => i == 0 || sortedSeries[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedSeries[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Series)
                .ToList();
        }

        public List<Series> GetAllSeries()
        {
            return _cache.Get("GetAllSeries", () => _seriesRepository.All().ToList(), TimeSpan.FromSeconds(30));
        }

        public Dictionary<int, List<int>> GetAllSeriesTags()
        {
            return _seriesRepository.AllSeriesTags();
        }

        public Dictionary<int, string> AllSeriesPaths()
        {
            return _seriesRepository.AllSeriesPaths();
        }

        public List<Series> AllForTag(int tagId)
        {
            return GetAllSeries().Where(s => s.Tags.Contains(tagId))
                                 .ToList();
        }

        public Series GetSeries(int seriesId)
        {
            return _seriesRepository.Get(seriesId);
        }

        public Series GetSeriesByMetadataId(int seriesMetadataId)
        {
            return _seriesRepository.GetSeriesByMetadataId(seriesMetadataId);
        }

        public List<Series> GetSeries(IEnumerable<int> seriesIds)
        {
            return _seriesRepository.Get(seriesIds).ToList();
        }

        public void RemoveAddOptions(Series series)
        {
            _seriesRepository.SetFields(series, s => s.AddOptions);
        }

        public Series UpdateSeries(Series series)
        {
            _cache.Clear();

            var storedSeries = GetSeries(series.Id);

            // Never update AddOptions when updating an series, keep it the same as the existing stored series.
            series.AddOptions = storedSeries.AddOptions;

            var updatedSeries = _seriesRepository.Update(series);
            _eventAggregator.PublishEvent(new SeriesEditedEvent(updatedSeries, storedSeries));

            return updatedSeries;
        }

        public List<Series> UpdateSeries(List<Series> series, bool useExistingRelativeFolder)
        {
            _cache.Clear();
            _logger.Debug("Updating {0} series", series.Count);

            foreach (var s in series)
            {
                _logger.Trace("Updating: {0}", s.Name);

                if (!s.RootFolderPath.IsNullOrWhiteSpace())
                {
                    s.Path = _seriesPathBuilder.BuildPath(s, useExistingRelativeFolder);

                    _logger.Trace("Changing path for {0} to {1}", s.Name, s.Path);
                }
                else
                {
                    _logger.Trace("Not changing path for: {0}", s.Name);
                }
            }

            _seriesRepository.UpdateMany(series);
            _logger.Debug("{0} series updated", series.Count);

            return series;
        }
    }
}
