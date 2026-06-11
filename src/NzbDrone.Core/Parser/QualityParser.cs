using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    public class QualityParser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(QualityParser));

        private static readonly Regex ProperRegex = new (@"\b(?<proper>proper)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RepackRegex = new (@"\b(?<repack>repack|rerip)\b",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex VersionRegex = new (@"\d[-._ ]?v(?<version>\d)[-._ ]|\[v(?<version>\d)\]",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Comic fix-release markers: "(f)", "(f2)", "(Fixed)"
        private static readonly Regex FixedRegex = new (@"\((?:f(?<fixversion>\d)?|fixed)\)",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RealRegex = new (@"\b(?<real>REAL)\b",
                                                                RegexOptions.Compiled);

        // Source tags rank a release; bare container tokens (CBZ/CBR/CB7) only
        // tell us it's an archive of unknown source. Checked in priority
        // order so "Digital" wins regardless of token position.
        private static readonly Regex DigitalRegex = new (@"\bdigital\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WebRipRegex = new (@"\bweb[-_. ]?rip\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex C2cRegex = new (@"\b(?:c2c|cover[ ._-]to[ ._-]cover)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScanRegex = new (@"\b(?:scan(?:ned)?|print)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ArchiveTokenRegex = new (@"\b(?:cbz|cbr|cb7)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PdfTokenRegex = new (@"\bpdf\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex EpubTokenRegex = new (@"\b(?:epub|mobi|azw3?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static QualityModel ParseQuality(string name, string desc = null, List<int> categories = null)
        {
            Logger.Debug("Trying to parse quality for '{0}'", name);

            if (name.IsNullOrWhiteSpace() && desc.IsNullOrWhiteSpace())
            {
                return new QualityModel { Quality = Quality.Unknown };
            }

            var normalizedName = name.Replace('_', ' ').Trim().ToLower();
            var result = ParseQualityModifiers(name, normalizedName);

            if (desc.IsNotNullOrWhiteSpace())
            {
                result.Quality = ParseSourceQuality(desc.ToLowerInvariant());

                if (result.Quality != Quality.Unknown)
                {
                    result.QualityDetectionSource = QualityDetectionSource.TagLib;
                    return result;
                }
            }

            result.Quality = ParseSourceQuality(normalizedName);

            //Based on extension
            if (result.Quality == Quality.Unknown && !name.ContainsInvalidPathChars())
            {
                try
                {
                    result.Quality = MediaFileExtensions.GetQualityForExtension(name.GetPathExtension());
                    result.QualityDetectionSource = QualityDetectionSource.Extension;
                }
                catch (ArgumentException)
                {
                    //Swallow exception for cases where string contains illegal
                    //path characters.
                }
            }

            return result;
        }

        public static Quality ParseSourceQuality(string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return Quality.Unknown;
            }

            if (DigitalRegex.IsMatch(name))
            {
                return Quality.Digital;
            }

            if (WebRipRegex.IsMatch(name))
            {
                return Quality.WebRip;
            }

            if (C2cRegex.IsMatch(name))
            {
                return Quality.C2C;
            }

            if (ScanRegex.IsMatch(name))
            {
                return Quality.Scan;
            }

            if (ArchiveTokenRegex.IsMatch(name))
            {
                return Quality.Archive;
            }

            if (PdfTokenRegex.IsMatch(name))
            {
                return Quality.PDF;
            }

            if (EpubTokenRegex.IsMatch(name))
            {
                return Quality.EPUB;
            }

            return Quality.Unknown;
        }

        internal static QualityModel ParseQualityModifiers(string name, string normalizedName)
        {
            var result = new QualityModel { Quality = Quality.Unknown };

            if (ProperRegex.IsMatch(normalizedName))
            {
                result.Revision.Version = 2;
            }

            if (RepackRegex.IsMatch(normalizedName))
            {
                result.Revision.Version = 2;
                result.Revision.IsRepack = true;
            }

            var versionRegexResult = VersionRegex.Match(normalizedName);

            if (versionRegexResult.Success)
            {
                result.Revision.Version = Convert.ToInt32(versionRegexResult.Groups["version"].Value);
            }

            var fixedRegexResult = FixedRegex.Match(normalizedName);

            if (fixedRegexResult.Success)
            {
                // "(f)" is the first fix (v2), "(f2)" the second (v3)
                var fixNumber = fixedRegexResult.Groups["fixversion"].Success
                    ? Convert.ToInt32(fixedRegexResult.Groups["fixversion"].Value)
                    : 1;

                result.Revision.Version = Math.Max(result.Revision.Version, fixNumber + 1);
            }

            //TODO: re-enable this when we have a reliable way to determine real
            var realRegexResult = RealRegex.Matches(name);

            if (realRegexResult.Count > 0)
            {
                result.Revision.Real = realRegexResult.Count;
            }

            return result;
        }
    }
}
