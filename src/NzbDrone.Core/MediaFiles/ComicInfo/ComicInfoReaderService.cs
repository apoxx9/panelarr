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
