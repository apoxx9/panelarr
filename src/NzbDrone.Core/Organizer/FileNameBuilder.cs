using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IBuildFileNames
    {
        string BuildComicFileName(Series series, Issue issue, ComicFile comicFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        string BuildComicFilePath(Series series, Issue issue, string fileName, string extension);
        string BuildIssuePath(Series series);
        BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec);
        string GetSeriesFolder(Series series, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IBuildFileNames
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IPublisherService _publisherService;
        private readonly ICached<IssueFormat[]> _issueFormatCache;
        private readonly Logger _logger;

        private static readonly Regex TitleRegex = new Regex(@"\{(?<prefix>[- ._\[(]*)(?<token>(?:[a-z0-9]+)(?:(?<separator>[- ._]+)(?:[a-z0-9]+))?)(?::(?<customFormat>[a-z0-9]+))?(?<suffix>[- ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex PartRegex = new Regex(@"\{(?<prefix>[^{]*?)(?<token1>PartNumber|PartCount)(?::(?<customFormat1>[a-z0-9]+))?(?<separator>.*(?=PartNumber|PartCount))?((?<token2>PartNumber|PartCount)(?::(?<customFormat2>[a-z0-9]+))?)?(?<suffix>[^}]*)\}",
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeasonEpisodePatternRegex = new Regex(@"(?<separator>(?<=})[- ._]+?)?(?<seasonEpisode>s?{season(?:\:0+)?}(?<episodeSeparator>[- ._]?[ex])(?<episode>{episode(?:\:0+)?}))(?<separator>[- ._]+?(?={))?",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeriesNameRegex = new Regex(@"(?<token>\{(?:Series)(?<separator>[- ._])(Clean)?(Sort)?(?:Name|Title)(The)?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex IssueTitleRegex = new Regex(@"(?<token>\{(?:Issue)(?<separator>[- ._])(Clean)?(?:Title|Number)(The)?(NoSub)?(?::(?<customFormat>[a-z0-9.#]+))?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FileNameCleanupRegex = new Regex(@"([- ._])(\1)+", RegexOptions.Compiled);
        private static readonly Regex TrimSeparatorsRegex = new Regex(@"[- ._]$", RegexOptions.Compiled);

        private static readonly Regex ScenifyRemoveChars = new Regex(@"(?<=\s)(,|<|>|\/|\\|;|:|'|""|\||`|~|!|\?|@|$|%|^|\*|-|_|=){1}(?=\s)|('|:|\?|,)(?=(?:(?:s|m)\s)|\s|$)|(\(|\)|\[|\]|\{|\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScenifyReplaceChars = new Regex(@"[\/]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TitlePrefixRegex = new Regex(@"^(The|An|A) (.*?)((?: *\([^)]+\))*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public FileNameBuilder(INamingConfigService namingConfigService,
                               IQualityDefinitionService qualityDefinitionService,
                               ICacheManager cacheManager,
                               ICustomFormatCalculationService formatCalculator,
                               IPublisherService publisherService,
                               Logger logger)
        {
            _namingConfigService = namingConfigService;
            _qualityDefinitionService = qualityDefinitionService;
            _formatCalculator = formatCalculator;
            _publisherService = publisherService;
            _issueFormatCache = cacheManager.GetCache<IssueFormat[]>(GetType(), "issueFormat");
            _logger = logger;
        }

        public string BuildComicFileName(Series series, Issue issue, ComicFile comicFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            if (!namingConfig.RenameComics)
            {
                return GetOriginalFileName(comicFile);
            }

            if (namingConfig.StandardIssueFormat.IsNullOrWhiteSpace())
            {
                throw new NamingFormatException("File name format cannot be empty");
            }

            var pattern = issue.IssueType switch
            {
                IssueType.Annual => namingConfig.AnnualIssueFormat.IsNullOrWhiteSpace()
                    ? namingConfig.StandardIssueFormat
                    : namingConfig.AnnualIssueFormat,
                IssueType.TPB or IssueType.Hardcover or IssueType.Omnibus => namingConfig.TPBFormat.IsNullOrWhiteSpace()
                    ? namingConfig.StandardIssueFormat
                    : namingConfig.TPBFormat,
                _ => namingConfig.StandardIssueFormat
            };

            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddSeriesTokens(tokenHandlers, series);
            AddIssueTokens(tokenHandlers, issue);
            AddComicFileTokens(tokenHandlers, comicFile);
            AddQualityTokens(tokenHandlers, series, comicFile);
            AddMediaInfoTokens(tokenHandlers, comicFile);
            AddCustomFormats(tokenHandlers, series, comicFile, customFormats);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplacePartTokens(splitPattern, tokenHandlers, namingConfig).Trim();
                component = ReplaceTokens(component, tokenHandlers, namingConfig).Trim();

                component = FileNameCleanupRegex.Replace(component, match => match.Captures[0].Value[0].ToString());
                component = TrimSeparatorsRegex.Replace(component, string.Empty);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public string BuildComicFilePath(Series series, Issue issue, string fileName, string extension)
        {
            Ensure.That(extension, () => extension).IsNotNullOrWhiteSpace();

            var path = BuildIssuePath(series);

            return Path.Combine(path, fileName + extension);
        }

        public string BuildIssuePath(Series series)
        {
            return series.Path;
        }

        public BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec)
        {
            var issueFormat = GetIssueFormat(nameSpec.StandardIssueFormat).LastOrDefault();

            if (issueFormat == null)
            {
                return new BasicNamingConfig();
            }

            var basicNamingConfig = new BasicNamingConfig
            {
                Separator = issueFormat.Separator
            };

            var titleTokens = TitleRegex.Matches(nameSpec.StandardIssueFormat);

            foreach (Match match in titleTokens)
            {
                var separator = match.Groups["separator"].Value;
                var token = match.Groups["token"].Value;

                if (!separator.Equals(" "))
                {
                    basicNamingConfig.ReplaceSpaces = true;
                }

                if (token.StartsWith("{Series", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeSeriesName = true;
                }

                if (token.StartsWith("{Issue", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeIssueTitle = true;
                }

                if (token.StartsWith("{Quality", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeQuality = true;
                }
            }

            return basicNamingConfig;
        }

        public string GetSeriesFolder(Series series, NamingConfig namingConfig = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            var pattern = namingConfig.SeriesFolderFormat;
            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddSeriesTokens(tokenHandlers, series);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig);
                component = CleanFolderName(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public static string CleanTitle(string title)
        {
            title = title.Replace("&", "and");
            title = ScenifyReplaceChars.Replace(title, " ");
            title = ScenifyRemoveChars.Replace(title, string.Empty);

            return title;
        }

        public static string TitleThe(string title)
        {
            return TitlePrefixRegex.Replace(title, "$2, $1$3");
        }

        public static string CleanFileName(string name)
        {
            return CleanFileName(name, NamingConfig.Default);
        }

        public static string CleanFolderName(string name)
        {
            name = FileNameCleanupRegex.Replace(name, match => match.Captures[0].Value[0].ToString());

            return name.Trim(' ', '.');
        }

        private void AddSeriesTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Series series)
        {
            tokenHandlers["{Series Name}"] = m => series.Name;
            tokenHandlers["{Series Title}"] = m => series.Name;
            tokenHandlers["{Series CleanName}"] = m => CleanTitle(series.Name);
            tokenHandlers["{Series NameThe}"] = m => TitleThe(series.Name);
            tokenHandlers["{Series SortName}"] = m => series?.Metadata?.Value?.SortName ?? string.Empty;
            tokenHandlers["{Series NameFirstCharacter}"] = m => TitleThe(series.Name).Substring(0, 1).FirstCharToUpper();

            if (series.Metadata.Value.Disambiguation != null)
            {
                tokenHandlers["{Series Disambiguation}"] = m => series.Metadata.Value.Disambiguation;
            }

            var seriesYear = series?.Metadata?.Value?.Year;
            tokenHandlers["{Series Year}"] = m => seriesYear.HasValue ? seriesYear.Value.ToString() : string.Empty;

            var publisherId = series?.Metadata?.Value?.PublisherId;
            if (publisherId.HasValue)
            {
                var publisher = _publisherService.GetPublisher(publisherId.Value);
                tokenHandlers["{Publisher}"] = m => publisher?.Name ?? string.Empty;
            }
            else
            {
                tokenHandlers["{Publisher}"] = m => string.Empty;
            }
        }

        private void AddIssueTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Issue issue)
        {
            tokenHandlers["{Issue Title}"] = m => issue.Title;
            tokenHandlers["{Issue CleanTitle}"] = m => CleanTitle(issue.Title);
            tokenHandlers["{Issue TitleThe}"] = m => TitleThe(issue.Title);

            var (titleNoSub, subtitle) = issue.Title.SplitIssueTitle(issue.SeriesMetadata.Value.Name);

            tokenHandlers["{Issue TitleNoSub}"] = m => titleNoSub;
            tokenHandlers["{Issue CleanTitleNoSub}"] = m => CleanTitle(titleNoSub);
            tokenHandlers["{Issue TitleTheNoSub}"] = m => TitleThe(titleNoSub);

            tokenHandlers["{Issue Subtitle}"] = m => subtitle;
            tokenHandlers["{Issue CleanSubtitle}"] = m => CleanTitle(subtitle);
            tokenHandlers["{Issue SubtitleThe}"] = m => TitleThe(subtitle);

            var seriesLinks = issue.SeriesLinks.Value;
            if (seriesLinks != null && seriesLinks.Any())
            {
                var primarySeries = seriesLinks.OrderBy(x => x.SeriesPosition).First();
                var seriesTitle = primarySeries.SeriesGroup?.Value?.Title + (primarySeries.Position.IsNotNullOrWhiteSpace() ? $" #{primarySeries.Position}" : string.Empty);

                tokenHandlers["{Issue SeriesGroup}"] = m => primarySeries.SeriesGroup.Value.Title;
                tokenHandlers["{Issue SeriesPosition}"] = m => primarySeries.Position;
                tokenHandlers["{Issue SeriesTitle}"] = m => seriesTitle;
            }

            tokenHandlers["{Issue Number}"] = m => issue.IssueNumber.ToString(m.CustomFormat ?? "0.##");
            tokenHandlers["{Issue Type}"] = m => issue.IssueType.ToString();

            var volumeNumber = issue.SeriesMetadata?.Value?.VolumeNumber;
            tokenHandlers["{Volume Number}"] = m => volumeNumber.HasValue
                ? volumeNumber.Value.ToString(m.CustomFormat ?? "0")
                : string.Empty;

            if (issue.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release Year}"] = m => issue.ReleaseDate.Value.Year.ToString();
                tokenHandlers["{Release YearFirst}"] = m => issue.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Release Year}"] = m => "Unknown";
                tokenHandlers["{Release YearFirst}"] = m => "Unknown";
            }
        }

        private void AddComicFileTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, ComicFile comicFile)
        {
            tokenHandlers["{Original Title}"] = m => GetOriginalTitle(comicFile);
            tokenHandlers["{Original Filename}"] = m => GetOriginalFileName(comicFile);
            tokenHandlers["{Release Group}"] = m => comicFile.ReleaseGroup ?? m.DefaultValue("Panelarr");

            if (comicFile.PartCount > 1)
            {
                tokenHandlers["{PartNumber}"] = m => comicFile.Part.ToString(m.CustomFormat);
                tokenHandlers["{PartCount}"] = m => comicFile.PartCount.ToString(m.CustomFormat);
            }
        }

        private void AddQualityTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Series series, ComicFile comicFile)
        {
            var qualityTitle = _qualityDefinitionService.Get(comicFile.Quality.Quality).Title;
            var qualityProper = GetQualityProper(comicFile.Quality);

            //var qualityReal = GetQualityReal(series, comicFile.Quality);
            tokenHandlers["{Quality Full}"] = m => string.Format("{0}", qualityTitle);
            tokenHandlers["{Quality Title}"] = m => qualityTitle;
            tokenHandlers["{Quality Proper}"] = m => qualityProper;

            //tokenHandlers["{Quality Real}"] = m => qualityReal;
        }

        private void AddMediaInfoTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, ComicFile comicFile)
        {
            // No media-info tokens for comic files — audio metadata is not relevant.
        }

        private void AddCustomFormats(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Series series, ComicFile comicFile, List<CustomFormat> customFormats = null)
        {
            if (customFormats == null)
            {
                comicFile.Series = series;
                customFormats = _formatCalculator.ParseCustomFormat(comicFile, series);
            }

            tokenHandlers["{Custom Formats}"] = m => string.Join(" ", customFormats.Where(x => x.IncludeCustomFormatWhenRenaming));
        }

        private string ReplaceTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return TitleRegex.Replace(pattern, match => ReplaceToken(match, tokenHandlers, namingConfig));
        }

        private string ReplaceToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenMatch = new TokenMatch
            {
                RegexMatch = match,
                Prefix = match.Groups["prefix"].Value,
                Separator = match.Groups["separator"].Value,
                Suffix = match.Groups["suffix"].Value,
                Token = match.Groups["token"].Value,
                CustomFormat = match.Groups["customFormat"].Value
            };

            if (tokenMatch.CustomFormat.IsNullOrWhiteSpace())
            {
                tokenMatch.CustomFormat = null;
            }

            var tokenHandler = tokenHandlers.GetValueOrDefault(tokenMatch.Token, m => string.Empty);

            var replacementText = tokenHandler(tokenMatch).Trim();

            if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsLower(t)))
            {
                replacementText = replacementText.ToLower();
            }
            else if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsUpper(t)))
            {
                replacementText = replacementText.ToUpper();
            }

            if (!tokenMatch.Separator.IsNullOrWhiteSpace())
            {
                replacementText = replacementText.Replace(" ", tokenMatch.Separator);
            }

            replacementText = CleanFileName(replacementText, namingConfig);

            if (!replacementText.IsNullOrWhiteSpace())
            {
                replacementText = tokenMatch.Prefix + replacementText + tokenMatch.Suffix;
            }

            return replacementText;
        }

        private string ReplacePartTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return PartRegex.Replace(pattern, match => ReplacePartToken(match, tokenHandlers, namingConfig));
        }

        private string ReplacePartToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token1"].Value}}}", m => string.Empty);

            var tokenText1 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat1"].Success ? match.Groups["customFormat1"].Value : "0" });

            if (tokenText1 == string.Empty)
            {
                return string.Empty;
            }

            var prefix = match.Groups["prefix"].Value;

            var tokenText2 = string.Empty;

            var separator = match.Groups["separator"].Success ? match.Groups["separator"].Value : string.Empty;

            var suffix = match.Groups["suffix"].Value;

            if (match.Groups["token2"].Success)
            {
                tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token2"].Value}}}", m => string.Empty);

                tokenText2 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat2"].Success ? match.Groups["customFormat2"].Value : "0" });
            }

            return $"{prefix}{tokenText1}{separator}{tokenText2}{suffix}";
        }

        private IssueFormat[] GetIssueFormat(string pattern)
        {
            return _issueFormatCache.Get(pattern, () => SeasonEpisodePatternRegex.Matches(pattern).OfType<Match>()
                .Select(match => new IssueFormat
                {
                    IssueSeparator = match.Groups["episodeSeparator"].Value,
                    Separator = match.Groups["separator"].Value,
                    IssuePattern = match.Groups["episode"].Value,
                }).ToArray());
        }

        private string GetQualityProper(QualityModel quality)
        {
            if (quality.Revision.Version > 1)
            {
                if (quality.Revision.IsRepack)
                {
                    return "Repack";
                }

                return "Proper";
            }

            return string.Empty;
        }

        private string GetOriginalTitle(ComicFile comicFile)
        {
            if (comicFile.SceneName.IsNullOrWhiteSpace())
            {
                return GetOriginalFileName(comicFile);
            }

            return comicFile.SceneName;
        }

        private string GetOriginalFileName(ComicFile comicFile)
        {
            return Path.GetFileNameWithoutExtension(comicFile.Path);
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            var result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "", "" };

            if (namingConfig.ReplaceIllegalCharacters)
            {
                // Smart replaces a colon followed by a space with space dash space for a better appearance
                if (namingConfig.ColonReplacementFormat == ColonReplacementFormat.Smart)
                {
                    result = result.Replace(": ", " - ");
                    result = result.Replace(":", "-");
                }
                else
                {
                    var replacement = string.Empty;

                    switch (namingConfig.ColonReplacementFormat)
                    {
                        case ColonReplacementFormat.Dash:
                            replacement = "-";
                            break;
                        case ColonReplacementFormat.SpaceDash:
                            replacement = " -";
                            break;
                        case ColonReplacementFormat.SpaceDashSpace:
                            replacement = " - ";
                            break;
                    }

                    result = result.Replace(":", replacement);
                }
            }
            else
            {
                result = result.Replace(":", string.Empty);
            }

            for (var i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], namingConfig.ReplaceIllegalCharacters ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }
    }

    internal sealed class TokenMatch
    {
        public Match RegexMatch { get; set; }
        public string Prefix { get; set; }
        public string Separator { get; set; }
        public string Suffix { get; set; }
        public string Token { get; set; }
        public string CustomFormat { get; set; }

        public string DefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
            {
                return defaultValue;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}
