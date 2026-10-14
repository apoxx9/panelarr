using System;
using System.IO;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles
{
    public static class ArchiveSignature
    {
        private const int HeaderLength = 8;

        // Comic archives routinely carry an extension that disagrees with
        // their container - a .cbr holding a ZIP, a .cbz holding RAR. Reading
        // the magic bytes lets the format be trusted over the name. Returns
        // null when the file is unreadable or holds no recognised signature,
        // which keeps a genuinely corrupt archive reported as a failure.
        public static ComicFormat? Identify(string filePath)
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var header = new byte[HeaderLength];
                    var read = stream.Read(header, 0, HeaderLength);

                    return Identify(header, read);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ComicFormat? Identify(byte[] header, int length)
        {
            if (header == null)
            {
                return null;
            }

            // "PK\x03\x04" and its empty/spanned variants. EPUB is also a ZIP,
            // but an EPUB never reaches a signature check because its handling
            // cannot fail.
            if (length >= 4 && header[0] == 0x50 && header[1] == 0x4B &&
                ((header[2] == 0x03 && header[3] == 0x04) ||
                 (header[2] == 0x05 && header[3] == 0x06) ||
                 (header[2] == 0x07 && header[3] == 0x08)))
            {
                return ComicFormat.CBZ;
            }

            // "Rar!" - RAR4 and RAR5 share the first four bytes
            if (length >= 4 && header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21)
            {
                return ComicFormat.CBR;
            }

            if (length >= 6 && header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC &&
                header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C)
            {
                return ComicFormat.CB7;
            }

            // "%PDF"
            if (length >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
            {
                return ComicFormat.PDF;
            }

            return null;
        }
    }
}
