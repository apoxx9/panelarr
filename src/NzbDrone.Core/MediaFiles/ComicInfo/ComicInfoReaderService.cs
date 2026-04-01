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
    public class ComicMetadataResult
    {
        public string Source { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();
    }

    public interface IComicInfoReaderService
    {
        List<ComicMetadataResult> ReadMetadata(ComicFile comicFile);
    }

    public class ComicInfoReaderService : IComicInfoReaderService
    {
        private static readonly string[] MetadataFileNames = { "ComicInfo.xml", "MetronInfo.xml" };
        private readonly Logger _logger;

        public ComicInfoReaderService(Logger logger)
        {
            _logger = logger;
        }

        public List<ComicMetadataResult> ReadMetadata(ComicFile comicFile)
        {
            var results = new List<ComicMetadataResult>();

            if (comicFile == null || string.IsNullOrWhiteSpace(comicFile.Path) || !File.Exists(comicFile.Path))
            {
                return results;
            }

            try
            {
                switch (comicFile.ComicFormat)
                {
                    case ComicFormat.CBZ:
                        results = ReadFromZip(comicFile.Path);
                        break;
                    case ComicFormat.CBR:
                        results = ReadFromRar(comicFile.Path);
                        break;
                    case ComicFormat.CB7:
                        results = ReadFrom7z(comicFile.Path);
                        break;
                    default:
                        _logger.Debug("Unsupported format for metadata reading: {0}", comicFile.ComicFormat);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read metadata from {0}", comicFile.Path);
            }

            return results;
        }

        private List<ComicMetadataResult> ReadFromZip(string path)
        {
            var results = new List<ComicMetadataResult>();

            using var archive = ZipFile.OpenRead(path);

            foreach (var name in MetadataFileNames)
            {
                var entry = archive.GetEntry(name);
                if (entry == null)
                {
                    continue;
                }

                using var stream = entry.Open();
                var result = ParseXml(stream, name);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private List<ComicMetadataResult> ReadFromRar(string path)
        {
            var results = new List<ComicMetadataResult>();

            using var archive = RarArchive.Open(path);

            foreach (var name in MetadataFileNames)
            {
                var entry = archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.Key, name, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    continue;
                }

                using var stream = entry.OpenEntryStream();
                var result = ParseXml(stream, name);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private List<ComicMetadataResult> ReadFrom7z(string path)
        {
            var results = new List<ComicMetadataResult>();

            using var archive = SevenZipArchive.Open(path);

            foreach (var name in MetadataFileNames)
            {
                var entry = archive.Entries.FirstOrDefault(e =>
                    string.Equals(e.Key, name, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    continue;
                }

                using var stream = entry.OpenEntryStream();
                var result = ParseXml(stream, name);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        private ComicMetadataResult ParseXml(Stream stream, string source)
        {
            try
            {
                // Use StreamReader to handle BOM and encoding issues
                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                var xml = reader.ReadToEnd();
                var doc = XDocument.Parse(xml);
                var result = new ComicMetadataResult
                {
                    Source = source,
                    Fields = new Dictionary<string, string>()
                };

                FlattenElement(doc.Root, "", result.Fields);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to parse {0}", source);
                return null;
            }
        }

        private void FlattenElement(XElement element, string prefix, Dictionary<string, string> fields)
        {
            var key = string.IsNullOrEmpty(prefix)
                ? element.Name.LocalName
                : $"{prefix} > {element.Name.LocalName}";

            if (!element.HasElements)
            {
                var value = element.Value.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    // Handle duplicate keys by appending index
                    if (fields.ContainsKey(key))
                    {
                        var index = 2;
                        while (fields.ContainsKey($"{key} ({index})"))
                        {
                            index++;
                        }

                        fields[$"{key} ({index})"] = value;
                    }
                    else
                    {
                        fields[key] = value;
                    }
                }

                // Also capture attributes
                foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    fields[$"{key} @{attr.Name.LocalName}"] = attr.Value;
                }
            }
            else
            {
                // Capture attributes on parent elements too
                foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    fields[$"{key} @{attr.Name.LocalName}"] = attr.Value;
                }

                foreach (var child in element.Elements())
                {
                    FlattenElement(child, element.Name.LocalName == element.Document?.Root?.Name.LocalName ? "" : key, fields);
                }
            }
        }
    }
}
