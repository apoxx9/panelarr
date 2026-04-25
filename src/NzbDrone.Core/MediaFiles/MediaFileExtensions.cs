using System;
using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaFileExtensions
    {
        private static readonly Dictionary<string, Quality> _comicExtensions;

        static MediaFileExtensions()
        {
            _comicExtensions = new Dictionary<string, Quality>(StringComparer.OrdinalIgnoreCase)
            {
                { ".cbz", Quality.CBZ },
                { ".cbr", Quality.CBR },
                { ".cb7", Quality.CB7 },
                { ".pdf", Quality.PDF },
            };
        }

        public static HashSet<string> Extensions => new HashSet<string>(_comicExtensions.Keys, StringComparer.OrdinalIgnoreCase);

        public static HashSet<string> TextExtensions => Extensions;

        public static HashSet<string> AudioExtensions => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static HashSet<string> AllExtensions => Extensions;

        public static Quality GetQualityForExtension(string extension)
        {
            if (_comicExtensions.ContainsKey(extension))
            {
                return _comicExtensions[extension];
            }

            return Quality.Unknown;
        }
    }
}
