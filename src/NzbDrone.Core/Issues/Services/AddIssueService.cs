using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.Issues
{
    public interface IAddIssueService
    {
        Issue AddIssue(Issue issue, bool doRefresh = true);
        List<Issue> AddIssues(List<Issue> issues, bool doRefresh = true);
    }

    public class AddIssueService : IAddIssueService
    {
        private readonly ISeriesService _seriesService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IIssueService _issueService;
        private readonly IProvideIssueInfo _issueInfoProvider;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public AddIssueService(ISeriesService seriesService,
                               IAddSeriesService addSeriesService,
                               IIssueService issueService,
                               IProvideIssueInfo issueInfoProvider,
                               IImportListExclusionService importListExclusionService,
                               Logger logger)
        {
            _seriesService = seriesService;
            _addSeriesService = addSeriesService;
            _issueService = issueService;
            _issueInfoProvider = issueInfoProvider;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        public Issue AddIssue(Issue issue, bool doRefresh = true)
        {
            _logger.Debug($"Adding issue {issue}");

            issue = AddSkyhookData(issue);

            // Check if the issue already exists
            var dbIssue = _issueService.FindById(issue.ForeignIssueId);
            if (dbIssue != null)
            {
                issue.UseDbFieldsFrom(dbIssue);
            }

            // Remove any import list exclusions preventing addition
            _importListExclusionService.Delete(issue.ForeignIssueId);
            _importListExclusionService.Delete(issue.SeriesMetadata.Value.ForeignSeriesId);

            // Note it's a manual addition so it's not deleted on next refresh
            issue.AddOptions.AddType = IssueAddType.Manual;

            // Add the series if necessary
            var dbSeries = _seriesService.FindById(issue.SeriesMetadata.Value.ForeignSeriesId);
            if (dbSeries == null)
            {
                var series = issue.Series.Value;

                series.Metadata.Value.ForeignSeriesId = issue.SeriesMetadata.Value.ForeignSeriesId;

                dbSeries = _addSeriesService.AddSeries(series, false);
            }

            issue.Series = dbSeries;
            issue.SeriesMetadataId = dbSeries.SeriesMetadataId;
            _issueService.AddIssue(issue, doRefresh);

            return issue;
        }

        public List<Issue> AddIssues(List<Issue> issues, bool doRefresh = true)
        {
            var added = DateTime.UtcNow;
            var addedIssues = new List<Issue>();

            foreach (var a in issues)
            {
                a.Added = added;
                try
                {
                    addedIssues.Add(AddIssue(a, doRefresh));
                }
                catch (Exception ex)
                {
                    // Could be a bad id from an import list
                    _logger.Error(ex, "Failed to import id: {0} - {1}", a.ForeignIssueId, a.Title);
                }
            }

            return addedIssues;
        }

        private Issue AddSkyhookData(Issue newIssue)
        {
            Tuple<string, Issue, List<SeriesMetadata>> tuple = null;
            try
            {
                tuple = _issueInfoProvider.GetIssueInfo(newIssue.ForeignIssueId);
            }
            catch (IssueNotFoundException)
            {
                _logger.Error("Issue with Foreign Id {0} was not found, it may have been removed from metadata.", newIssue.ForeignIssueId);

                throw new ValidationException(new List<ValidationFailure>
                                              {
                                                  new ValidationFailure("ForeignIssueId", "A issue with this ID was not found", newIssue.ForeignIssueId)
                                              });
            }

            newIssue.UseMetadataFrom(tuple.Item2);
            newIssue.Added = DateTime.UtcNow;

            var metadata = tuple.Item3.FirstOrDefault(x => x.ForeignSeriesId == tuple.Item1);
            newIssue.SeriesMetadata = metadata;

            return newIssue;
        }
    }
}
