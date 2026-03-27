using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Books;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteBook remoteBook, long size);
        List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series artist);
        List<CustomFormat> ParseCustomFormat(ComicFile comicFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Series artist);
        List<CustomFormat> ParseCustomFormat(EntityHistory history, Series artist);
        List<CustomFormat> ParseCustomFormat(LocalBook localBook);
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

        public List<CustomFormat> ParseCustomFormat(RemoteBook remoteBook, long size)
        {
            var input = new CustomFormatInput
            {
                BookInfo = remoteBook.ParsedBookInfo,
                Series = remoteBook.Series,
                Size = size,
                IndexerFlags = remoteBook.Release?.IndexerFlags ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series author)
        {
            return ParseCustomFormat(comicFile, author, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(ComicFile comicFile)
        {
            return ParseCustomFormat(comicFile, comicFile.Series.Value, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Series author)
        {
            var parsed = Parser.Parser.ParseBookTitle(blocklist.SourceTitle);

            var bookInfo = new ParsedBookInfo
            {
                SeriesName = author.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Quality = blocklist.Quality,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Series = author,
                Size = blocklist.Size ?? 0,
                IndexerFlags = blocklist.IndexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Series author)
        {
            var parsed = Parser.Parser.ParseBookTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);
            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags indexerFlags);

            var bookInfo = new ParsedBookInfo
            {
                SeriesName = author.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Quality = history.Quality,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Series = author,
                Size = size,
                IndexerFlags = indexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalBook localBook)
        {
            var bookInfo = new ParsedBookInfo
            {
                SeriesName = localBook.Series.Name,
                ReleaseTitle = localBook.SceneName,
                Quality = localBook.Quality,
                ReleaseGroup = localBook.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Series = localBook.Series,
                Size = localBook.Size,
                IndexerFlags = localBook.IndexerFlags,
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

        private List<CustomFormat> ParseCustomFormat(ComicFile comicFile, Series author, List<CustomFormat> allCustomFormats)
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

            var bookInfo = new ParsedBookInfo
            {
                SeriesName = author.Name,
                ReleaseTitle = releaseTitle,
                Quality = comicFile.Quality,
                ReleaseGroup = comicFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Series = author,
                Size = comicFile.Size,
                IndexerFlags = comicFile.IndexerFlags,
                Filename = Path.GetFileName(comicFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
