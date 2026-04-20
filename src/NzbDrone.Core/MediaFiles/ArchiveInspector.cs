using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Core.Issues;
using PdfSharpCore.Pdf.IO;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SixLabors.ImageSharp;

namespace NzbDrone.Core.MediaFiles
{
    public class ArchiveInspectionResult
    {
        public int PageCount { get; set; }
        public int ImageCount { get; set; }
        public float ImageQualityScore { get; set; }
        public string Error { get; set; }
    }

    public interface IArchiveInspector
    {
        ArchiveInspectionResult Inspect(string filePath, ComicFormat format);
    }

    public class ArchiveInspector : IArchiveInspector
    {
        private const long BaselinePixels = 1920L * 1080L;
        private const int MaxSampleSize = 10;

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly Logger _logger;

        public ArchiveInspector(Logger logger)
        {
            _logger = logger;
        }

        public ArchiveInspectionResult Inspect(string filePath, ComicFormat format)
        {
            try
            {
                switch (format)
                {
                    case ComicFormat.CBZ:
                        return InspectZipArchive(filePath);
                    case ComicFormat.CBR:
                        return InspectRarArchive(filePath);
                    case ComicFormat.CB7:
                        return InspectSevenZipArchive(filePath);
                    case ComicFormat.PDF:
                        return InspectPdf(filePath);
                    case ComicFormat.EPUB:
                        return new ArchiveInspectionResult
                        {
                            PageCount = 0,
                            ImageCount = 0,
                            ImageQualityScore = 0.5f
                        };
                    default:
                        return new ArchiveInspectionResult { Error = string.Format("Unsupported format: {0}", format) };
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Archive inspection failed for {0}", filePath);
                return new ArchiveInspectionResult { Error = ex.Message };
            }
        }

        private ArchiveInspectionResult InspectZipArchive(string filePath)
        {
            using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(filePath))
            {
                return InspectEntries(archive.Entries.Where(e => !e.IsDirectory));
            }
        }

        private ArchiveInspectionResult InspectRarArchive(string filePath)
        {
            using (var archive = RarArchive.Open(filePath))
            {
                return InspectEntries(archive.Entries.Where(e => !e.IsDirectory));
            }
        }

        private ArchiveInspectionResult InspectSevenZipArchive(string filePath)
        {
            using (var archive = SevenZipArchive.Open(filePath))
            {
                return InspectEntries(archive.Entries.Where(e => !e.IsDirectory));
            }
        }

        private ArchiveInspectionResult InspectEntries(IEnumerable<IArchiveEntry> entries)
        {
            var imageEntries = entries
                .Where(e => ImageExtensions.Contains(Path.GetExtension(e.Key)))
                .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var imageCount = imageEntries.Count;
            if (imageCount == 0)
            {
                return new ArchiveInspectionResult
                {
                    PageCount = 0,
                    ImageCount = 0,
                    ImageQualityScore = 0.0f
                };
            }

            var sampleIndices = GetSampleIndices(imageCount, MaxSampleSize);
            var qualityScores = new List<double>();

            foreach (var idx in sampleIndices)
            {
                var entry = imageEntries[idx];
                try
                {
                    using (var entryStream = entry.OpenEntryStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            entryStream.CopyTo(ms);
                            ms.Position = 0;

                            var (width, height) = ReadImageDimensions(ms, Path.GetExtension(entry.Key));
                            if (width > 0 && height > 0)
                            {
                                var pixels = (double)width * height;
                                var score = Math.Min(pixels / BaselinePixels, 1.0);
                                qualityScores.Add(score);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Could not read dimensions for entry {0}", entry.Key);
                }
            }

            var avgQuality = qualityScores.Count > 0
                ? (float)qualityScores.Average()
                : 0.0f;

            return new ArchiveInspectionResult
            {
                PageCount = imageCount,
                ImageCount = imageCount,
                ImageQualityScore = avgQuality
            };
        }

        private ArchiveInspectionResult InspectPdf(string filePath)
        {
            try
            {
                using (var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.InformationOnly))
                {
                    return new ArchiveInspectionResult
                    {
                        PageCount = doc.PageCount,
                        ImageCount = 0,
                        ImageQualityScore = 0.5f
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to read page count from PDF {0}", filePath);
                return new ArchiveInspectionResult
                {
                    PageCount = 0,
                    ImageCount = 0,
                    ImageQualityScore = 0.5f,
                    Error = ex.Message
                };
            }
        }

        private static (int width, int height) ReadImageDimensions(Stream stream, string extension)
        {
            var ext = extension.ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg")
            {
                return ReadJpegDimensions(stream);
            }

            if (ext == ".png")
            {
                return ReadPngDimensions(stream);
            }

            stream.Position = 0;
            var info = Image.Identify(stream);
            return info != null ? (info.Width, info.Height) : (0, 0);
        }

        private static (int width, int height) ReadJpegDimensions(Stream stream)
        {
            var buf2 = new byte[2];
            if (stream.Read(buf2, 0, 2) < 2 || buf2[0] != 0xFF || buf2[1] != 0xD8)
            {
                return (0, 0);
            }

            var markerBuf = new byte[4];
            while (stream.Position < stream.Length - 8)
            {
                if (stream.Read(markerBuf, 0, 2) < 2)
                {
                    break;
                }

                if (markerBuf[0] != 0xFF)
                {
                    break;
                }

                var marker = markerBuf[1];

                while (marker == 0xFF)
                {
                    var b = stream.ReadByte();
                    if (b < 0)
                    {
                        return (0, 0);
                    }

                    marker = (byte)b;
                }

                if ((marker >= 0xC0 && marker <= 0xC3) ||
                    (marker >= 0xC5 && marker <= 0xC7) ||
                    (marker >= 0xC9 && marker <= 0xCB) ||
                    (marker >= 0xCD && marker <= 0xCF))
                {
                    var sofBuf = new byte[7];
                    if (stream.Read(sofBuf, 0, 7) < 7)
                    {
                        break;
                    }

                    var h = (sofBuf[3] << 8) | sofBuf[4];
                    var w = (sofBuf[5] << 8) | sofBuf[6];
                    return (w, h);
                }

                if (stream.Read(markerBuf, 0, 2) < 2)
                {
                    break;
                }

                var segLen = (markerBuf[0] << 8) | markerBuf[1];
                if (segLen < 2)
                {
                    break;
                }

                stream.Seek(segLen - 2, SeekOrigin.Current);
            }

            return (0, 0);
        }

        private static (int width, int height) ReadPngDimensions(Stream stream)
        {
            const int headerLen = 8 + 4 + 4 + 4 + 4;
            var buf = new byte[headerLen];
            if (stream.Read(buf, 0, headerLen) < headerLen)
            {
                return (0, 0);
            }

            if (buf[0] != 0x89 || buf[1] != 0x50 || buf[2] != 0x4E || buf[3] != 0x47)
            {
                return (0, 0);
            }

            var w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
            var h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
            return (w, h);
        }

        private static IReadOnlyList<int> GetSampleIndices(int total, int maxSamples)
        {
            if (total <= maxSamples)
            {
                return Enumerable.Range(0, total).ToList();
            }

            var indices = new List<int>(maxSamples);
            for (var i = 0; i < maxSamples; i++)
            {
                indices.Add((int)Math.Round((double)i * (total - 1) / (maxSamples - 1)));
            }

            return indices;
        }
    }
}
