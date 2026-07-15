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
    public class ComicFormatConversionResult
    {
        public string FinalPath { get; set; }
        public bool Changed { get; set; }
        public string Error { get; set; }
    }

    public interface IComicFormatConverter
    {
        /// <summary>
        /// Detects if a file with a .cbz extension is actually a RAR or 7z archive,
        /// and converts it to a real ZIP-based CBZ. Returns the (possibly new) file path.
        /// </summary>
        string NormalizeToCbz(string filePath);

        /// <summary>
        /// Converts any comic archive to a real ZIP-based .cbz: RAR/7z content is
        /// repacked (verified before the original is replaced), zip content with a
        /// wrong extension is renamed. The Mylar convert-before-tag convention -
        /// nothing can write metadata into a RAR.
        /// </summary>
        ComicFormatConversionResult ConvertToRealCbz(string filePath);
    }

    public class ComicFormatConverter : IComicFormatConverter
    {
        private readonly Logger _logger;

        public ComicFormatConverter(Logger logger)
        {
            _logger = logger;
        }

        public ComicFormatConversionResult ConvertToRealCbz(string filePath)
        {
            var result = new ComicFormatConversionResult { FinalPath = filePath };

            if (!File.Exists(filePath))
            {
                result.Error = "file does not exist";
                return result;
            }

            var format = DetectArchiveFormat(filePath);
            var isCbzExtension = filePath.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase);
            var targetPath = Path.ChangeExtension(filePath, ".cbz");

            if (format == ArchiveType.Zip)
            {
                if (isCbzExtension)
                {
                    return result;
                }

                // Zip content wearing a .cbr/.cb7 extension - a rename is the
                // whole conversion
                if (File.Exists(targetPath))
                {
                    result.Error = $"target already exists: {targetPath}";
                    return result;
                }

                File.Move(filePath, targetPath);
                _logger.Info("ComicFormatConverter: Renamed zip-content {0} to {1}", filePath, targetPath);
                result.FinalPath = targetPath;
                result.Changed = true;
                return result;
            }

            if (format == ArchiveType.Rar || format == ArchiveType.SevenZip)
            {
                if (!targetPath.Equals(filePath, StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath))
                {
                    result.Error = $"target already exists: {targetPath}";
                    return result;
                }

                var converted = RepackToCbz(filePath, targetPath, format, out var error);

                if (error != null)
                {
                    result.Error = error;
                    return result;
                }

                result.FinalPath = converted;
                result.Changed = true;
                return result;
            }

            result.Error = $"unknown archive format";
            return result;
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
                var repacked = RepackToCbz(filePath, filePath, format, out var error);
                return error == null ? repacked : filePath;
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

        private string RepackToCbz(string sourcePath, string targetPath, ArchiveType sourceFormat, out string error)
        {
            // .partial~ keeps the temp file invisible to library scans
            var tempPath = targetPath + ".partial~";
            error = null;

            try
            {
                int sourceEntryCount;

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

                        sourceEntryCount = entries.Count;

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

                // The original is only deleted after the repack proves readable
                // and complete - a truncated NFS write must never eat a file
                using (var verify = ZipFile.OpenRead(tempPath))
                {
                    var repackedCount = verify.Entries.Count(e => !e.FullName.EndsWith("/", StringComparison.Ordinal));

                    if (repackedCount != sourceEntryCount)
                    {
                        throw new InvalidOperationException($"verification failed: {repackedCount} entries repacked, {sourceEntryCount} in source");
                    }
                }

                File.Delete(sourcePath);
                File.Move(tempPath, targetPath);

                _logger.Info("ComicFormatConverter: Successfully converted {0} -> CBZ: {1}", sourceFormat, targetPath);
                return targetPath;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ComicFormatConverter: Failed to convert {0}", sourcePath);
                error = ex.Message;

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
