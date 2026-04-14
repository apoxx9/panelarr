using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NLog;
using NzbDrone.Core.Issues;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public interface ICreditExtractorService
    {
        List<Credit> ExtractCredits(ComicFile comicFile);
    }

    public class CreditExtractorService : ICreditExtractorService
    {
        private readonly Logger _logger;

        public CreditExtractorService(Logger logger)
        {
            _logger = logger;
        }

        public List<Credit> ExtractCredits(ComicFile comicFile)
        {
            if (comicFile == null || string.IsNullOrWhiteSpace(comicFile.Path) || !File.Exists(comicFile.Path))
            {
                return new List<Credit>();
            }

            try
            {
                return comicFile.ComicFormat switch
                {
                    ComicFormat.CBZ => ExtractFromZip(comicFile.Path),
                    ComicFormat.CBR => ExtractFromRar(comicFile.Path),
                    ComicFormat.CB7 => ExtractFrom7z(comicFile.Path),
                    _ => new List<Credit>()
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to extract credits from {0}", comicFile.Path);
                return new List<Credit>();
            }
        }

        private List<Credit> ExtractFromZip(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            var entry = archive.GetEntry("ComicInfo.xml");

            if (entry == null)
            {
                return new List<Credit>();
            }

            using var stream = entry.Open();
            return ParseCredits(stream);
        }

        private List<Credit> ExtractFromRar(string path)
        {
            using var archive = RarArchive.Open(path);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.Key, "ComicInfo.xml", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return new List<Credit>();
            }

            using var stream = entry.OpenEntryStream();
            return ParseCredits(stream);
        }

        private List<Credit> ExtractFrom7z(string path)
        {
            using var archive = SevenZipArchive.Open(path);
            var entry = archive.Entries.FirstOrDefault(e =>
                string.Equals(e.Key, "ComicInfo.xml", StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                return new List<Credit>();
            }

            using var stream = entry.OpenEntryStream();
            return ParseCredits(stream);
        }

        private List<Credit> ParseCredits(Stream stream)
        {
            var credits = new List<Credit>();

            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var xml = reader.ReadToEnd();
            var doc = XDocument.Parse(xml);
            var root = doc.Root;

            if (root == null)
            {
                return credits;
            }

            foreach (var (elementName, role) in ComicInfoCreditRoles.ElementToRole)
            {
                var element = root.Element(elementName);
                var value = element?.Value?.Trim();

                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                // ComicInfo.xml uses comma-separated names for multiple people in the same role
                var names = value.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var name in names)
                {
                    var trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        credits.Add(new Credit { PersonName = trimmed, Role = role });
                    }
                }
            }

            return credits;
        }
    }
}
