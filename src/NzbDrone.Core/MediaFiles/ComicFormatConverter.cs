using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NLog;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;

namespace NzbDrone.Core.MediaFiles
{
    public interface IComicFormatConverter
    {
        /// <summary>
        /// Detects if a file with a .cbz extension is actually a RAR or 7z archive,
        /// and converts it to a real ZIP-based CBZ. Returns the (possibly new) file path.
        /// </summary>
        string NormalizeToCbz(string filePath);
    }

    public class ComicFormatConverter : IComicFormatConverter
    {
        private readonly Logger _logger;

        public ComicFormatConverter(Logger logger)
        {
            _logger = logger;
        }

        public string NormalizeToCbz(string filePath)
        {
            if (!filePath.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase))
            {
                return filePath;
            }

            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var format = DetectArchiveFormat(filePath);

            if (format == ArchiveType.Zip)
            {
                // Already a real CBZ
                return filePath;
            }

            if (format == ArchiveType.Rar || format == ArchiveType.SevenZip)
            {
                _logger.Info("ComicFormatConverter: Detected {0} archive mislabeled as .cbz, converting: {1}", format, filePath);
                return ConvertToCbz(filePath, format);
            }

            _logger.Warn("ComicFormatConverter: Unknown archive format for {0}, leaving as-is", filePath);
            return filePath;
        }

        private ArchiveType DetectArchiveFormat(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                var header = new byte[8];
                var bytesRead = stream.Read(header, 0, header.Length);

                if (bytesRead < 4)
                {
                    return ArchiveType.Unknown;
                }

                // ZIP: PK\x03\x04
                if (header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04)
                {
                    return ArchiveType.Zip;
                }

                // RAR: Rar!\x1A\x07
                if (header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21)
                {
                    return ArchiveType.Rar;
                }

                // 7z: 7z\xBC\xAF\x27\x1C
                if (bytesRead >= 6 && header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC && header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C)
                {
                    return ArchiveType.SevenZip;
                }

                return ArchiveType.Unknown;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "ComicFormatConverter: Failed to detect format for {0}", filePath);
                return ArchiveType.Unknown;
            }
        }

        private string ConvertToCbz(string sourcePath, ArchiveType sourceFormat)
        {
            var tempPath = sourcePath + ".converting.cbz";

            try
            {
                using (var zipStream = new FileStream(tempPath, FileMode.Create))
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    IArchive sourceArchive = sourceFormat switch
                    {
                        ArchiveType.Rar => RarArchive.Open(sourcePath),
                        ArchiveType.SevenZip => SevenZipArchive.Open(sourcePath),
                        _ => throw new InvalidOperationException($"Unsupported source format: {sourceFormat}")
                    };

                    using (sourceArchive)
                    {
                        var entries = sourceArchive.Entries
                            .Where(e => !e.IsDirectory)
                            .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        _logger.Info("ComicFormatConverter: Repacking {0} entries from {1} to CBZ", entries.Count, sourceFormat);

                        foreach (var entry in entries)
                        {
                            var zipEntry = zipArchive.CreateEntry(entry.Key, CompressionLevel.Fastest);
                            using var entryStream = entry.OpenEntryStream();
                            using var zipEntryStream = zipEntry.Open();
                            entryStream.CopyTo(zipEntryStream);
                        }
                    }
                }

                // Replace original with converted file
                File.Delete(sourcePath);
                File.Move(tempPath, sourcePath);

                _logger.Info("ComicFormatConverter: Successfully converted {0} → CBZ: {1}", sourceFormat, sourcePath);
                return sourcePath;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ComicFormatConverter: Failed to convert {0}", sourcePath);

                // Clean up temp file on failure
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Best effort
                    }
                }

                return sourcePath;
            }
        }

        private enum ArchiveType
        {
            Unknown,
            Zip,
            Rar,
            SevenZip
        }
    }
}
