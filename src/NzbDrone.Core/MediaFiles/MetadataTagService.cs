using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedFileTagInfo ReadTags(IFileInfo file);
        void WriteTags(ComicFile comicFile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId);
        List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagSeriesCommand>
    {
        private readonly IComicInfoReaderService _comicInfoReaderService;
        private readonly IConfigService _configService;
        private readonly IComicInfoGenerator _comicInfoGenerator;
        private readonly IComicInfoEmbedService _comicInfoEmbedService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IIssueService _issueService;
        private readonly IPublisherService _publisherService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MetadataTagService(IComicInfoReaderService comicInfoReaderService,
            IConfigService configService,
            IComicInfoGenerator comicInfoGenerator,
            IComicInfoEmbedService comicInfoEmbedService,
            IMediaFileService mediaFileService,
            IIssueService issueService,
            IPublisherService publisherService,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _comicInfoReaderService = comicInfoReaderService;
            _configService = configService;
            _comicInfoGenerator = comicInfoGenerator;
            _comicInfoEmbedService = comicInfoEmbedService;
            _mediaFileService = mediaFileService;
            _issueService = issueService;
            _publisherService = publisherService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ParsedFileTagInfo ReadTags(IFileInfo file)
        {
            return ReadComicTags(file);
        }

        private ParsedFileTagInfo ReadComicTags(IFileInfo file)
        {
            var info = new ParsedFileTagInfo();

            try
            {
                var ident = _comicInfoReaderService.ReadIdentificationFromPath(file.FullName);
                if (ident == null || !ident.HasAny)
                {
                    return info;
                }

                if (!string.IsNullOrWhiteSpace(ident.Series))
                {
                    info.Series = new List<string> { ident.Series };
                    info.SeriesTitle = ident.Series;
                    info.CleanTitle = ident.Series.CleanSeriesName();
                }

                if (!string.IsNullOrWhiteSpace(ident.Title))
                {
                    info.IssueTitle = ident.Title;
                    info.Title = ident.Title;
                }

                if (!string.IsNullOrWhiteSpace(ident.Number))
                {
                    info.SeriesIndex = ident.Number;
                }

                if (!string.IsNullOrWhiteSpace(ident.Year)
                    && uint.TryParse(ident.Year, out var year))
                {
                    info.Year = year;
                }

                if (!string.IsNullOrWhiteSpace(ident.Publisher))
                {
                    info.Publisher = ident.Publisher;
                }

                info.ForeignIssueId = ExtractComicVineIssueId(ident.Web, ident.Notes);

                _logger.Debug(
                    "Read ComicInfo identification from {0}: series='{1}', issue='{2}', number='{3}', foreignId='{4}'",
                    file.Name,
                    ident.Series,
                    ident.Title,
                    ident.Number,
                    info.ForeignIssueId);
            }
            catch (System.Exception ex)
            {
                _logger.Warn(ex, "Failed to read ComicInfo from {0}", file.FullName);
            }

            return info;
        }

        // Taggers (Mylar via ComicTagger) embed the ComicVine issue id in the
        // Web url ("…/4000-<id>/") and in Notes ("[CVDB<id>]", no separator,
        // or "[Issue ID <id>]").
        private static readonly System.Text.RegularExpressions.Regex WebIssueIdRegex =
            new (@"comicvine\.gamespot\.com/.*?/4000-(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex NotesIssueIdRegex =
            new (@"\[(?:CVDB|Issue ID)[:\s]*(\d+)\]", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        internal static string ExtractComicVineIssueId(string web, string notes)
        {
            if (!string.IsNullOrWhiteSpace(web))
            {
                var match = WebIssueIdRegex.Match(web);
                if (match.Success)
                {
                    return "cv:" + match.Groups[1].Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(notes))
            {
                var match = NotesIssueIdRegex.Match(notes);
                if (match.Success)
                {
                    return "cv:" + match.Groups[1].Value;
                }
            }

            return null;
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
            // Honor the Write Issue Tags setting: with the default (NewFiles),
            // importing an existing library in place must NOT rewrite the
            // files' tags — embedding currently replaces ComicInfo.xml wholesale
            // and destroys tagger provenance (e.g. Mylar's ComicVine ids).
            if (!force)
            {
                var setting = _configService.WriteIssueTags;

                if (setting == WriteIssueTagsType.NewFiles && !newDownload)
                {
                    _logger.Debug("Skipping tag embed for existing file {0} (WriteIssueTags=NewFiles)", comicFile.Path);
                    return;
                }
            }

            _comicInfoEmbedService.EmbedMetadata(comicFile);
        }

        public void SyncTags(List<Issue> issues)
        {
            // Comic files use ComicInfo.xml embedded during import — no audio sync needed
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId)
        {
            var comicFiles = _mediaFileService.GetFilesBySeries(seriesId);

            return GetRetagPreviews(comicFiles);
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            var comicFiles = _mediaFileService.GetFilesByIssue(issueId);

            return GetRetagPreviews(comicFiles);
        }

        private List<RetagComicFilePreview> GetRetagPreviews(List<ComicFile> comicFiles)
        {
            var previews = new List<RetagComicFilePreview>();

            foreach (var comicFile in comicFiles)
            {
                if (comicFile.IssueId == 0 || comicFile.ComicFormat != Issues.ComicFormat.CBZ)
                {
                    continue;
                }

                if (!System.IO.File.Exists(comicFile.Path))
                {
                    continue;
                }

                try
                {
                    var preview = GetRetagPreview(comicFile);
                    if (preview != null)
                    {
                        previews.Add(preview);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to generate retag preview for {0}", comicFile.Path);
                }
            }

            return previews;
        }

        private RetagComicFilePreview GetRetagPreview(ComicFile comicFile)
        {
            // Read current embedded ComicInfo.xml fields
            var currentResults = _comicInfoReaderService.ReadMetadata(comicFile);
            var currentFields = currentResults
                .FirstOrDefault(r => r.Source.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
                ?.Fields ?? new Dictionary<string, string>();

            // Generate what the new ComicInfo.xml would contain
            var issue = _issueService.GetIssue(comicFile.IssueId);
            if (issue == null)
            {
                return null;
            }

            var seriesMetadata = issue.SeriesMetadata?.Value;
            Publisher publisher = null;

            if (seriesMetadata?.PublisherId.HasValue == true)
            {
                publisher = _publisherService.GetPublisher(seriesMetadata.PublisherId.Value);
            }

            var newXml = _comicInfoGenerator.Generate(issue, seriesMetadata, publisher);
            var newResult = _comicInfoReaderService.ParseXmlContent(newXml, "ComicInfo.xml");
            var newFields = newResult?.Fields ?? new Dictionary<string, string>();

            // Diff field by field
            var allKeys = currentFields.Keys.Union(newFields.Keys).Distinct();
            var changes = new Dictionary<string, Tuple<string, string>>();

            foreach (var key in allKeys)
            {
                currentFields.TryGetValue(key, out var oldVal);
                newFields.TryGetValue(key, out var newVal);

                if (!string.Equals(oldVal ?? string.Empty, newVal ?? string.Empty, StringComparison.Ordinal))
                {
                    changes[key] = Tuple.Create(oldVal ?? string.Empty, newVal ?? string.Empty);
                }
            }

            if (!changes.Any())
            {
                return null;
            }

            return new RetagComicFilePreview
            {
                SeriesId = issue.SeriesId,
                IssueId = comicFile.IssueId,
                ComicFileId = comicFile.Id,
                Path = comicFile.Path,
                Changes = changes
            };
        }

        public void Execute(RetagFilesCommand message)
        {
            foreach (var fileId in message.Files)
            {
                try
                {
                    var comicFile = _mediaFileService.Get(fileId);
                    if (comicFile.ComicFormat != ComicFormat.Unknown)
                    {
                        // explicit user command — embed regardless of the setting
                        _comicInfoEmbedService.EmbedMetadata(comicFile);
                    }
                }
                catch (NzbDrone.Core.Datastore.ModelNotFoundException)
                {
                    _logger.Warn("ComicFile {0} not found, skipping retag", fileId);
                }
            }
        }

        public void Execute(RetagSeriesCommand message)
        {
            var allSeries = message.SeriesIds;

            foreach (var seriesId in allSeries)
            {
                var comicFiles = _mediaFileService.GetFilesBySeries(seriesId);

                _logger.Info("Re-tagging {0} comic files for series {1}", comicFiles.Count, seriesId);

                foreach (var file in comicFiles.Where(x => x.ComicFormat != ComicFormat.Unknown))
                {
                    // explicit user command — embed regardless of the setting
                    _comicInfoEmbedService.EmbedMetadata(file);
                }
            }
        }
    }
}
