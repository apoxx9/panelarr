using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Metadata
{
    public interface IMetadataOverrideService
    {
        void SaveSeriesOverride(int seriesMetadataId, Dictionary<string, object> fields);
        void ClearSeriesOverride(int seriesMetadataId);
        void SaveIssueOverride(int issueId, Dictionary<string, object> fields);
        void ClearIssueOverride(int issueId);
    }

    public class MetadataOverrideService : IMetadataOverrideService
    {
        private readonly ISeriesMetadataRepository _seriesMetadataRepository;
        private readonly IBookRepository _bookRepository;
        private readonly Logger _logger;

        // Fields that can be overridden on SeriesMetadata
        private static readonly HashSet<string> AllowedSeriesFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "SortName", "Overview", "Status", "SeriesType", "Year",
            "VolumeNumber", "PublisherId", "Genres", "Ratings", "Images", "Disambiguation"
        };

        // Fields that can be overridden on Issue
        private static readonly HashSet<string> AllowedIssueFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Title", "IssueNumber", "IssueType", "ReleaseDate", "PageCount",
            "CoverArtUrl", "Genres", "Ratings"
        };

        public MetadataOverrideService(ISeriesMetadataRepository seriesMetadataRepository,
                                       IBookRepository bookRepository,
                                       Logger logger)
        {
            _seriesMetadataRepository = seriesMetadataRepository;
            _bookRepository = bookRepository;
            _logger = logger;
        }

        public void SaveSeriesOverride(int seriesMetadataId, Dictionary<string, object> fields)
        {
            var metadata = _seriesMetadataRepository.Get(seriesMetadataId);
            if (metadata == null)
            {
                throw new ArgumentException($"SeriesMetadata {seriesMetadataId} not found");
            }

            var overriddenFieldNames = new List<string>(
                (metadata.OverriddenFields ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var kvp in fields)
            {
                var fieldName = kvp.Key;
                if (!AllowedSeriesFields.Contains(fieldName))
                {
                    _logger.Warn("Ignoring unknown override field: {0}", fieldName);
                    continue;
                }

                ApplyFieldToSeriesMetadata(metadata, fieldName, kvp.Value);

                if (!overriddenFieldNames.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                {
                    overriddenFieldNames.Add(fieldName);
                }
            }

            metadata.IsOverridden = true;
            metadata.OverriddenFields = string.Join(",", overriddenFieldNames);

            _seriesMetadataRepository.Update(metadata);
            _logger.Debug("Saved metadata override for SeriesMetadata {0}: {1}", seriesMetadataId, metadata.OverriddenFields);
        }

        public void ClearSeriesOverride(int seriesMetadataId)
        {
            var metadata = _seriesMetadataRepository.Get(seriesMetadataId);
            if (metadata == null)
            {
                throw new ArgumentException($"SeriesMetadata {seriesMetadataId} not found");
            }

            metadata.IsOverridden = false;
            metadata.OverriddenFields = null;
            _seriesMetadataRepository.Update(metadata);
            _logger.Debug("Cleared metadata override for SeriesMetadata {0}", seriesMetadataId);
        }

        public void SaveIssueOverride(int issueId, Dictionary<string, object> fields)
        {
            var issue = _bookRepository.Get(issueId);
            if (issue == null)
            {
                throw new ArgumentException($"Issue {issueId} not found");
            }

            var overriddenFieldNames = new List<string>(
                (issue.OverriddenFields ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var kvp in fields)
            {
                var fieldName = kvp.Key;
                if (!AllowedIssueFields.Contains(fieldName))
                {
                    _logger.Warn("Ignoring unknown override field: {0}", fieldName);
                    continue;
                }

                ApplyFieldToIssue(issue, fieldName, kvp.Value);

                if (!overriddenFieldNames.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                {
                    overriddenFieldNames.Add(fieldName);
                }
            }

            issue.IsOverridden = true;
            issue.OverriddenFields = string.Join(",", overriddenFieldNames);

            _bookRepository.Update(issue);
            _logger.Debug("Saved metadata override for Issue {0}: {1}", issueId, issue.OverriddenFields);
        }

        public void ClearIssueOverride(int issueId)
        {
            var issue = _bookRepository.Get(issueId);
            if (issue == null)
            {
                throw new ArgumentException($"Issue {issueId} not found");
            }

            issue.IsOverridden = false;
            issue.OverriddenFields = null;
            _bookRepository.Update(issue);
            _logger.Debug("Cleared metadata override for Issue {0}", issueId);
        }

        private static void ApplyFieldToSeriesMetadata(SeriesMetadata metadata, string fieldName, object value)
        {
            switch (fieldName)
            {
                case "Name":
                    metadata.Name = Convert.ToString(value);
                    break;
                case "SortName":
                    metadata.SortName = Convert.ToString(value);
                    break;
                case "Overview":
                    metadata.Overview = Convert.ToString(value);
                    break;
                case "Status":
                    if (Enum.TryParse<SeriesStatusType>(Convert.ToString(value), true, out var status))
                    {
                        metadata.Status = status;
                    }

                    break;
                case "SeriesType":
                    if (Enum.TryParse<SeriesType>(Convert.ToString(value), true, out var seriesType))
                    {
                        metadata.SeriesType = seriesType;
                    }

                    break;
                case "Year":
                    metadata.Year = value == null ? (int?)null : Convert.ToInt32(value);
                    break;
                case "VolumeNumber":
                    metadata.VolumeNumber = value == null ? (int?)null : Convert.ToInt32(value);
                    break;
                case "PublisherId":
                    metadata.PublisherId = value == null ? (int?)null : Convert.ToInt32(value);
                    break;
                case "Disambiguation":
                    metadata.Disambiguation = Convert.ToString(value);
                    break;
            }
        }

        private static void ApplyFieldToIssue(Issue issue, string fieldName, object value)
        {
            switch (fieldName)
            {
                case "Title":
                    issue.Title = Convert.ToString(value);
                    break;
                case "IssueNumber":
                    issue.IssueNumber = Convert.ToSingle(value);
                    break;
                case "IssueType":
                    if (Enum.TryParse<IssueType>(Convert.ToString(value), true, out var issueType))
                    {
                        issue.IssueType = issueType;
                    }

                    break;
                case "ReleaseDate":
                    issue.ReleaseDate = value == null ? (DateTime?)null : Convert.ToDateTime(value);
                    break;
                case "PageCount":
                    issue.PageCount = Convert.ToInt32(value);
                    break;
                case "CoverArtUrl":
                    issue.CoverArtUrl = Convert.ToString(value);
                    break;
            }
        }
    }
}
