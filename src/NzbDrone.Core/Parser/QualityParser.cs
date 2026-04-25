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

        private static readonly Regex RealRegex = new (@"\b(?<real>REAL)\b",
                                                                RegexOptions.Compiled);

        private static readonly Regex CodecRegex = new (@"\b(?:(?<CBZ_HD>CBZ\s*HD)|(?<CBZ_Web>CBZ\s*Web)|(?<CBZ>CBZ)|(?<CBR>CBR)|(?<CB7>CB7)|(?<PDF>PDF)|(?<MOBI>MOBI)|(?<EPUB>EPUB)|(?<AZW3>AZW3?))\b",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                var descCodec = ParseCodec(desc, "");
                Logger.Trace($"Got codec {descCodec}");

                result.Quality = FindQuality(descCodec);

                if (result.Quality != Quality.Unknown)
                {
                    result.QualityDetectionSource = QualityDetectionSource.TagLib;
                    return result;
                }
            }

            var codec = ParseCodec(normalizedName, name);

            result.Quality = FindQuality(codec);

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

        public static Codec ParseCodec(string name, string origName)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return Codec.Unknown;
            }

            var match = CodecRegex.Match(name);

            if (!match.Success)
            {
                return Codec.Unknown;
            }

            if (match.Groups["CBZ_HD"].Success)
            {
                return Codec.CBZ_HD;
            }

            if (match.Groups["CBZ_Web"].Success)
            {
                return Codec.CBZ_Web;
            }

            if (match.Groups["CBZ"].Success)
            {
                return Codec.CBZ;
            }

            if (match.Groups["CBR"].Success)
            {
                return Codec.CBR;
            }

            if (match.Groups["CB7"].Success)
            {
                return Codec.CB7;
            }

            if (match.Groups["PDF"].Success)
            {
                return Codec.PDF;
            }

            if (match.Groups["EPUB"].Success)
            {
                return Codec.EPUB;
            }

            if (match.Groups["MOBI"].Success)
            {
                return Codec.MOBI;
            }

            if (match.Groups["AZW3"].Success)
            {
                return Codec.AZW3;
            }

            return Codec.Unknown;
        }

        private static Quality FindQuality(Codec codec)
        {
            switch (codec)
            {
                case Codec.CBZ_HD:
                    return Quality.CBZ_HD;
                case Codec.CBZ_Web:
                    return Quality.CBZ_Web;
                case Codec.CBZ:
                    return Quality.CBZ;
                case Codec.CBR:
                    return Quality.CBR;
                case Codec.CB7:
                    return Quality.CB7;
                case Codec.PDF:
                    return Quality.PDF;
                case Codec.EPUB:
                    return Quality.EPUB;
                default:
                    return Quality.Unknown;
            }
        }

        private static QualityModel ParseQualityModifiers(string name, string normalizedName)
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

            //TODO: re-enable this when we have a reliable way to determine real
            var realRegexResult = RealRegex.Matches(name);

            if (realRegexResult.Count > 0)
            {
                result.Revision.Real = realRegexResult.Count;
            }

            return result;
        }
    }

    public enum Codec
    {
        CBZ,
        CBZ_HD,
        CBZ_Web,
        CBR,
        CB7,
        PDF,
        EPUB,
        MOBI,
        AZW3,
        Unknown
    }
}
