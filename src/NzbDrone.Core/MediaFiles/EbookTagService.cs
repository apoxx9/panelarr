using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Azw;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using PdfSharpCore.Pdf.IO;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.MediaFiles
{
    public interface IEBookTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(ComicFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int authorId);
        List<RetagComicFilePreview> GetRetagPreviewsByBook(int bookId);
        void RetagFiles(RetagFilesCommand message);
        void RetagSeries(RetagSeriesCommand message);
    }

    public class EBookTagService : IEBookTagService
    {
        private readonly ISeriesService _authorService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IConfigService _configService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public EBookTagService(ISeriesService authorService,
            IMediaFileService mediaFileService,
            IRootFolderService rootFolderService,
            IConfigService configService,
            ICalibreProxy calibre,
            Logger logger)
        {
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _rootFolderService = rootFolderService;
            _configService = configService;
            _calibre = calibre;

            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            var extension = file.Extension.ToLower();
            _logger.Trace($"Got extension '{extension}'");

            switch (extension)
            {
                case ".pdf":
                    return ReadPdf(file.FullName);
                case ".epub":
                case ".kepub":
                    return ReadEpub(file.FullName);
                case ".azw3":
                case ".mobi":
                    return ReadAzw3(file.FullName);
                default:
                    return Parser.Parser.ParseTitle(file.FullName);
            }
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
            if (!force)
            {
                if (_configService.WriteBookTags == WriteBookTagsType.NewFiles && !newDownload)
                {
                    return;
                }
            }

            _logger.Debug($"Writing tags for {comicFile}");

            WriteTagsInternal(comicFile, _configService.UpdateCovers, _configService.EmbedMetadata);
        }

        public void SyncTags(List<Issue> issues)
        {
            if (_configService.WriteBookTags != WriteBookTagsType.Sync)
            {
                return;
            }

            // get the tracks to update
            foreach (var issue in issues)
            {
                var comicFiles = issue.ComicFiles.Value;

                _logger.Debug($"Syncing ebook tags for {issue}");

                foreach (var file in comicFiles)
                {
                    // populate issue reference
                    file.Issue = issue;

                    WriteTagsInternal(file, _configService.UpdateCovers, _configService.EmbedMetadata);
                }
            }
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int authorId)
        {
            var files = _mediaFileService.GetFilesBySeries(authorId);

            return GetPreviews(files).ToList();
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            var files = _mediaFileService.GetFilesByBook(bookId);

            return GetPreviews(files).ToList();
        }

        public void RetagFiles(RetagFilesCommand message)
        {
            var author = _authorService.GetSeries(message.SeriesId);
            var files = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Re-tagging {0} ebook files for {1}", files.Count, author.Name);

            foreach (var file in files)
            {
                WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
            }

            _logger.ProgressInfo("Selected ebook files re-tagged for {0}", author.Name);
        }

        public void RetagSeries(RetagSeriesCommand message)
        {
            _logger.Debug("Re-tagging all ebook files for selected authors");
            var authorsToRename = _authorService.GetSeriess(message.SeriesIds);

            foreach (var author in authorsToRename)
            {
                var files = _mediaFileService.GetFilesBySeries(author.Id);

                _logger.ProgressInfo("Re-tagging all ebook files for author: {0}", author.Name);

                foreach (var file in files)
                {
                    WriteTagsInternal(file, message.UpdateCovers, message.EmbedMetadata);
                }

                _logger.ProgressInfo("All ebook files re-tagged for {0}", author.Name);
            }
        }

        private void WriteTagsInternal(ComicFile file, bool updateCover, bool embedMetadata)
        {
            var rootFolder = _rootFolderService.GetBestRootFolder(file.Path);

            if (rootFolder == null)
            {
                throw new Exception($"File '{file.Path}' is not in a root folder.");
            }

            _calibre.SetFields(file, rootFolder.CalibreSettings, updateCover, embedMetadata);
        }

        private IEnumerable<RetagComicFilePreview> GetPreviews(List<ComicFile> files)
        {
            // Calibre integration removed - no previews without CalibreId
            return Enumerable.Empty<RetagComicFilePreview>();
        }

        private ParsedTrackInfo ReadEpub(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                using (var bookRef = EpubReader.OpenBook(file))
                {
                    result.Seriess = bookRef.SeriesList;
                    result.IssueTitle = bookRef.Title;

                    var meta = bookRef.Schema.Package.Metadata;

                    _logger.Trace(meta.ToJson());

                    result.Isbn = GetIsbn(meta?.Identifiers);
                    result.Asin = meta?.Identifiers?.FirstOrDefault(x => x.Scheme?.ToLower().Contains("asin") ?? false)?.Identifier;
                    result.Language = meta?.Languages?.FirstOrDefault();
                    result.Publisher = meta?.Publishers?.FirstOrDefault();
                    result.Disambiguation = meta?.Description;

                    result.SeriesTitle = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series")?.Content;
                    result.SeriesIndex = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series_index")?.Content;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading epub");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadAzw3(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo();

            try
            {
                var issue = new Azw3File(file);
                result.Seriess = issue.Seriess;
                result.IssueTitle = issue.Title;
                result.Isbn = StripIsbn(issue.Isbn);
                result.Asin = issue.Asin;
                result.Language = issue.Language;
                result.Disambiguation = issue.Description;
                result.Publisher = issue.Publisher;
                result.Label = issue.Imprint;
                result.Source = issue.Source;

                result.Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading file");

                result.Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.Extension
                };
            }

            _logger.Trace($"Got {result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadPdf(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.PDF,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                var issue = PdfReader.Open(file, PdfDocumentOpenMode.InformationOnly);
                if (issue.Info != null)
                {
                    result.Seriess = new List<string> { issue.Info.Subject };
                    result.IssueTitle = issue.Info.Title;

                    _logger.Trace(issue.Info.ToJson());
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading pdf");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        public string GetIsbn(IEnumerable<EpubMetadataIdentifier> ids)
        {
            var candidates = ids.Select(x => StripIsbn(x?.Identifier))
                .Where(x => x != null)
                .OrderByDescending(x => x.Length);

            return candidates.FirstOrDefault(x => x.StartsWith("978"))
                ?? candidates.FirstOrDefault(x => x.StartsWith("979"))
                ?? candidates.FirstOrDefault();
        }

        private string GetIsbnChars(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new string(input.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
        }

        private string StripIsbn(string input)
        {
            var isbn = GetIsbnChars(input);

            if (isbn == null)
            {
                return null;
            }
            else if ((isbn.Length == 10 && ValidateIsbn10(isbn)) ||
                (isbn.Length == 13 && ValidateIsbn13(isbn)))
            {
                return isbn;
            }

            return null;
        }

        private static char Isbn10Checksum(string isbn)
        {
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += int.Parse(isbn[i].ToString()) * (10 - i);
            }

            var result = sum % 11;

            if (result == 0)
            {
                return '0';
            }
            else if (result == 1)
            {
                return 'X';
            }

            return (11 - result).ToString()[0];
        }

        private static char Isbn13Checksum(string isbn)
        {
            var result = 0;
            for (var i = 0; i < 12; i++)
            {
                result += int.Parse(isbn[i].ToString()) * ((i % 2 == 0) ? 1 : 3);
            }

            result %= 10;

            return result == 0 ? '0' : (10 - result).ToString()[0];
        }

        private static bool ValidateIsbn10(string isbn)
        {
            return ulong.TryParse(isbn.Substring(0, 9), out _) && isbn[9] == Isbn10Checksum(isbn);
        }

        private static bool ValidateIsbn13(string isbn)
        {
            return ulong.TryParse(isbn, out _) && isbn[12] == Isbn13Checksum(isbn);
        }
    }
}
