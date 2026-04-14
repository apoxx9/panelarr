using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteIssue remoteIssue, long size);
        List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series artist);
        List<CustomFormat> ParseCustomFormat(ComicFile comicFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Series artist);
        List<CustomFormat> ParseCustomFormat(EntityHistory history, Series artist);
        List<CustomFormat> ParseCustomFormat(LocalIssue localIssue);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService, Logger logger)
        {
            _formatService = formatService;
            _logger = logger;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteIssue remoteIssue, long size)
        {
            var input = new CustomFormatInput
            {
                IssueInfo = remoteIssue.ParsedIssueInfo,
                Series = remoteIssue.Series,
                Size = size,
                IndexerFlags = remoteIssue.Release?.IndexerFlags ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series series)
        {
            return ParseCustomFormat(comicFile, series, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(ComicFile comicFile)
        {
            return ParseCustomFormat(comicFile, comicFile.Series.Value, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Series series)
        {
            var parsed = Parser.Parser.ParseIssueTitle(blocklist.SourceTitle);

            var parsedInfo = new ParsedIssueInfo
            {
                SeriesName = series.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Quality = blocklist.Quality,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = parsedInfo,
                Series = series,
                Size = blocklist.Size ?? 0,
                IndexerFlags = blocklist.IndexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Series series)
        {
            var parsed = Parser.Parser.ParseIssueTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);
            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags indexerFlags);

            var parsedInfo = new ParsedIssueInfo
            {
                SeriesName = series.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Quality = history.Quality,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                IssueInfo = parsedInfo,
                Series = series,
                Size = size,
                IndexerFlags = indexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalIssue localIssue)
        {
            var parsedInfo = new ParsedIssueInfo
            {
                SeriesName = localIssue.Series.Name,
                ReleaseTitle = localIssue.SceneName,
                Quality = localIssue.Quality,
                ReleaseGroup = localIssue.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = parsedInfo,
                Series = localIssue.Series,
                Size = localIssue.Size,
                IndexerFlags = localIssue.IndexerFlags,
            };

            return ParseCustomFormat(input);
        }

        private List<CustomFormat> ParseCustomFormat(CustomFormatInput input)
        {
            return ParseCustomFormat(input, _formatService.All());
        }

        private static List<CustomFormat> ParseCustomFormat(CustomFormatInput input, List<CustomFormat> allCustomFormats)
        {
            var matches = new List<CustomFormat>();

            foreach (var customFormat in allCustomFormats)
            {
                var specificationMatches = customFormat.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(input))
                    })
                    .ToList();

                if (specificationMatches.All(x => x.DidMatch))
                {
                    matches.Add(customFormat);
                }
            }

            return matches.OrderBy(x => x.Name).ToList();
        }

        private List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series series, List<CustomFormat> allCustomFormats)
        {
            var releaseTitle = string.Empty;

            if (comicFile.SceneName.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using scene name for release title: {0}", comicFile.SceneName);
                releaseTitle = comicFile.SceneName;
            }
            else if (comicFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using original file path for release title: {0}", comicFile.OriginalFilePath);
                releaseTitle = comicFile.OriginalFilePath;
            }
            else if (comicFile.Path.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using path for release title: {0}", Path.GetFileName(comicFile.Path));
                releaseTitle = Path.GetFileName(comicFile.Path);
            }

            var parsedInfo = new ParsedIssueInfo
            {
                SeriesName = series.Name,
                ReleaseTitle = releaseTitle,
                Quality = comicFile.Quality,
                ReleaseGroup = comicFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                IssueInfo = parsedInfo,
                Series = series,
                Size = comicFile.Size,
                IndexerFlags = comicFile.IndexerFlags,
                Filename = Path.GetFileName(comicFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
