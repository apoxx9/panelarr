using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Books
{
    public interface ISeriesService
    {
        Series GetSeries(int authorId);
        Series GetSeriesByMetadataId(int authorMetadataId);
        List<Series> GetSeriess(IEnumerable<int> authorIds);
        Series AddSeries(Series newSeries, bool doRefresh);
        List<Series> AddSeries(List<Series> newSeriess, bool doRefresh);
        Series FindById(string foreignSeriesId);
        Series FindByName(string title);
        Series FindByNameInexact(string title);
        List<Series> GetCandidates(string title);
        List<Series> GetReportCandidates(string reportTitle);
        void DeleteSeries(int authorId, bool deleteFiles, bool addImportListExclusion = false);
        List<Series> GetAllSeries();
        Dictionary<int, List<int>> GetAllSeriesTags();
        List<Series> AllForTag(int tagId);
        Series UpdateSeries(Series author);
        List<Series> UpdateSeriess(List<Series> authors, bool useExistingRelativeFolder);
        Dictionary<int, string> AllSeriesPaths();
        bool SeriesPathExists(string folder);
        void RemoveAddOptions(Series author);
    }

    public class SeriesService : ISeriesService
    {
        private readonly ISeriesRepository _authorRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildSeriesPaths _authorPathBuilder;
        private readonly Logger _logger;
        private readonly ICached<List<Series>> _cache;

        public SeriesService(ISeriesRepository authorRepository,
                             IEventAggregator eventAggregator,
                             IBuildSeriesPaths authorPathBuilder,
                             ICacheManager cacheManager,
                             Logger logger)
        {
            _authorRepository = authorRepository;
            _eventAggregator = eventAggregator;
            _authorPathBuilder = authorPathBuilder;
            _cache = cacheManager.GetRollingCache<List<Series>>(GetType(), "authorcache", TimeSpan.FromSeconds(30));
            _logger = logger;
        }

        public Series AddSeries(Series newSeries, bool doRefresh)
        {
            _cache.Clear();
            _authorRepository.Insert(newSeries);
            _eventAggregator.PublishEvent(new SeriesAddedEvent(GetSeries(newSeries.Id), doRefresh));

            return newSeries;
        }

        public List<Series> AddSeries(List<Series> newSeriess, bool doRefresh)
        {
            _cache.Clear();
            _authorRepository.InsertMany(newSeriess);
            _eventAggregator.PublishEvent(new SeriesImportedEvent(newSeriess.Select(s => s.Id).ToList(), doRefresh));

            return newSeriess;
        }

        public bool SeriesPathExists(string folder)
        {
            return _authorRepository.SeriesPathExists(folder);
        }

        public void DeleteSeries(int authorId, bool deleteFiles, bool addImportListExclusion = false)
        {
            _cache.Clear();
            var author = _authorRepository.Get(authorId);
            _authorRepository.Delete(authorId);
            _eventAggregator.PublishEvent(new SeriesDeletedEvent(author, deleteFiles, addImportListExclusion));
        }

        public Series FindById(string foreignSeriesId)
        {
            return _authorRepository.FindById(foreignSeriesId);
        }

        public Series FindByName(string title)
        {
            return _authorRepository.FindByName(title.CleanSeriesName());
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
            var authors = GetAllSeries();

            foreach (var func in SeriesScoringFunctions(title, title.CleanSeriesName()))
            {
                var results = FindByStringInexact(authors, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Series> GetCandidates(string title)
        {
            var authors = GetAllSeries();
            var output = new List<Series>();

            foreach (var func in SeriesScoringFunctions(title, title.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(authors, func.Item1, func.Item2));
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
            var authors = GetAllSeries();
            var output = new List<Series>();

            foreach (var func in ReportSeriesScoringFunctions(reportTitle, reportTitle.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(authors, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        private List<Series> FindByStringInexact(List<Series> authors, Func<Series, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.8;
            const double fuzzGap = 0.2;

            var sortedSeriess = authors.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Series = s
            })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedSeriess.TakeWhile((x, i) => i == 0 || sortedSeriess[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedSeriess[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Series)
                .ToList();
        }

        public List<Series> GetAllSeries()
        {
            return _cache.Get("GetAllSeries", () => _authorRepository.All().ToList(), TimeSpan.FromSeconds(30));
        }

        public Dictionary<int, List<int>> GetAllSeriesTags()
        {
            return _authorRepository.AllSeriesTags();
        }

        public Dictionary<int, string> AllSeriesPaths()
        {
            return _authorRepository.AllSeriesPaths();
        }

        public List<Series> AllForTag(int tagId)
        {
            return GetAllSeries().Where(s => s.Tags.Contains(tagId))
                                 .ToList();
        }

        public Series GetSeries(int authorId)
        {
            return _authorRepository.Get(authorId);
        }

        public Series GetSeriesByMetadataId(int authorMetadataId)
        {
            return _authorRepository.GetSeriesByMetadataId(authorMetadataId);
        }

        public List<Series> GetSeriess(IEnumerable<int> authorIds)
        {
            return _authorRepository.Get(authorIds).ToList();
        }

        public void RemoveAddOptions(Series author)
        {
            _authorRepository.SetFields(author, s => s.AddOptions);
        }

        public Series UpdateSeries(Series author)
        {
            _cache.Clear();

            var storedSeries = GetSeries(author.Id);

            // Never update AddOptions when updating an author, keep it the same as the existing stored author.
            author.AddOptions = storedSeries.AddOptions;

            var updatedSeries = _authorRepository.Update(author);
            _eventAggregator.PublishEvent(new SeriesEditedEvent(updatedSeries, storedSeries));

            return updatedSeries;
        }

        public List<Series> UpdateSeriess(List<Series> author, bool useExistingRelativeFolder)
        {
            _cache.Clear();
            _logger.Debug("Updating {0} author", author.Count);

            foreach (var s in author)
            {
                _logger.Trace("Updating: {0}", s.Name);

                if (!s.RootFolderPath.IsNullOrWhiteSpace())
                {
                    s.Path = _authorPathBuilder.BuildPath(s, useExistingRelativeFolder);

                    _logger.Trace("Changing path for {0} to {1}", s.Name, s.Path);
                }
                else
                {
                    _logger.Trace("Not changing path for: {0}", s.Name);
                }
            }

            _authorRepository.UpdateMany(author);
            _logger.Debug("{0} authors updated", author.Count);

            return author;
        }
    }
}
