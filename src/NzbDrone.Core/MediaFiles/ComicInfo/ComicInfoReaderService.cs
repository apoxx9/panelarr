using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

    public class ComicInfoIdentification
    {
        public string Series { get; set; }
        public string Title { get; set; }
        public string Number { get; set; }

        // Mylar/ComicTagger write the series START YEAR here — this is a year
        // hint, never a volume number.
        public string Volume { get; set; }
        public string Year { get; set; }
        public string Publisher { get; set; }

        // Carry the ComicVine issue id: taggers embed it in the Web url
        // ("…/4000-<id>/") and in Notes ("[CVDB<id>]" / "[Issue ID <id>]").
        public string Web { get; set; }
        public string Notes { get; set; }
        public bool HasAny => !string.IsNullOrWhiteSpace(Series)
            || !string.IsNullOrWhiteSpace(Title)
            || !string.IsNullOrWhiteSpace(Number);
    }

    public interface IComicInfoReaderService
    {
        List<ComicMetadataResult> ReadMetadata(ComicFile comicFile);
        ComicInfoIdentification ReadIdentificationFromPath(string path);
        ComicMetadataResult ParseXmlContent(string xmlContent, string source);
    }

    public class ComicInfoReaderService : IComicInfoReaderService
    {
        private static readonly string[] MetadataFileNames = { "ComicInfo.xml", "MetronInfo.xml" };
        private readonly Logger _logger;

        public ComicInfoReaderService(Logger logger)
        {
            _logger = logger;
        }

        public ComicInfoIdentification ReadIdentificationFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            var format = GetFormatFromExtension(path);
            if (format == ComicFormat.Unknown)
            {
                return null;
            }

            var fakeFile = new ComicFile { Path = path, ComicFormat = format };
            var results = ReadMetadata(fakeFile);

            // Prefer ComicInfo.xml; fall back to MetronInfo.xml
            var preferred = results.FirstOrDefault(r => r.Source.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
                ?? results.FirstOrDefault();

            if (preferred == null)
            {
                return null;
            }

            // Field names are humanized at the root: "Series" stays "Series", "StartYear" becomes "Start Year"
            return new ComicInfoIdentification
            {
                Series = GetField(preferred.Fields, "Series"),
                Title = GetField(preferred.Fields, "Title"),
                Number = GetField(preferred.Fields, "Number"),
                Volume = GetField(preferred.Fields, "Volume"),
                Year = GetField(preferred.Fields, "Year") ?? GetField(preferred.Fields, "Start Year"),
                Publisher = GetField(preferred.Fields, "Publisher"),
                Web = GetField(preferred.Fields, "Web"),
                Notes = GetField(preferred.Fields, "Notes")
            };
        }

        private static ComicFormat GetFormatFromExtension(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            return ext switch
            {
                ".cbz" => ComicFormat.CBZ,
                ".cbr" => ComicFormat.CBR,
                ".cb7" => ComicFormat.CB7,
                _ => ComicFormat.Unknown
            };
        }

        private static string GetField(Dictionary<string, string> fields, string key)
        {
            return fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
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
                results = ReadAs(comicFile.Path, comicFile.ComicFormat);
            }
            catch (Exception ex)
            {
                // The extension can disagree with the container. Only pay for
                // the signature read once the declared format has failed, then
                // retry as what the file actually is.
                var sniffed = ArchiveSignature.Identify(comicFile.Path);

                if (sniffed.HasValue && sniffed.Value != comicFile.ComicFormat)
                {
                    try
                    {
                        results = ReadAs(comicFile.Path, sniffed.Value);
                        _logger.Debug("{0} is named {1} but holds a {2} archive - read as {2}", comicFile.Path, comicFile.ComicFormat, sniffed.Value);
                        return results;
                    }
                    catch (Exception retryEx)
                    {
                        _logger.Error(retryEx, "Failed to read metadata from {0}, including as {1}", comicFile.Path, sniffed.Value);
                        return results;
                    }
                }

                _logger.Error(ex, "Failed to read metadata from {0}", comicFile.Path);
            }

            return results;
        }

        private List<ComicMetadataResult> ReadAs(string path, ComicFormat format)
        {
            switch (format)
            {
                case ComicFormat.CBZ:
                    return ReadFromZip(path);
                case ComicFormat.CBR:
                    return ReadFromRar(path);
                case ComicFormat.CB7:
                    return ReadFrom7z(path);
                default:
                    _logger.Debug("Unsupported format for metadata reading: {0}", format);
                    return new List<ComicMetadataResult>();
            }
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

        public ComicMetadataResult ParseXmlContent(string xmlContent, string source)
        {
            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return null;
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlContent));
            return ParseXml(stream, source);
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
            var name = HumanizeElementName(element.Name.LocalName);
            var key = string.IsNullOrEmpty(prefix)
                ? name
                : $"{prefix} - {name}";

            if (!element.HasElements)
            {
                var value = element.Value.Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    // If the element has a qualifying attribute (e.g. <ID source="metron">),
                    // incorporate it into the display key for clarity
                    var qualifier = element.Attributes()
                        .FirstOrDefault(a => !a.IsNamespaceDeclaration);

                    var displayKey = qualifier != null
                        ? $"{key} ({qualifier.Value})"
                        : key;

                    AddField(fields, displayKey, value);
                }
                else
                {
                    // Element has no text value — show attributes as standalone fields
                    foreach (var attr in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                    {
                        AddField(fields, $"{key} - {HumanizeElementName(attr.Name.LocalName)}", attr.Value);
                    }
                }
            }
            else
            {
                // Recurse into children, skipping the root element prefix
                var childPrefix = element.Name.LocalName == element.Document?.Root?.Name.LocalName
                    ? ""
                    : key;

                // If this parent has a single child with the same base name (e.g. IDs > ID),
                // collapse the nesting to avoid redundant labels
                var children = element.Elements().ToList();
                var isSingleChildWrapper = children.Count == 1 &&
                    children[0].Name.LocalName.TrimEnd('s') == element.Name.LocalName.TrimEnd('s');

                if (isSingleChildWrapper && string.IsNullOrEmpty(childPrefix))
                {
                    // Root-level wrapper: skip entirely
                    FlattenElement(children[0], "", fields);
                }
                else if (isSingleChildWrapper)
                {
                    // Nested wrapper: use parent name for the child
                    FlattenElement(children[0], prefix, fields);
                }
                else
                {
                    foreach (var child in children)
                    {
                        FlattenElement(child, childPrefix, fields);
                    }
                }
            }
        }

        private static string HumanizeElementName(string name)
        {
            // Convert PascalCase/camelCase to spaced words:
            // "StartYear" -> "Start Year", "PageCount" -> "Page Count"
            // Leave already-spaced or short names alone
            if (string.IsNullOrEmpty(name) || name.Length <= 2)
            {
                return name;
            }

            var result = new StringBuilder();
            result.Append(name[0]);

            for (var i = 1; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                {
                    result.Append(' ');
                }

                result.Append(name[i]);
            }

            return result.ToString();
        }

        private static void AddField(Dictionary<string, string> fields, string key, string value)
        {
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
    }
}
