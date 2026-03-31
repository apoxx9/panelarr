using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsParser : IParseIndexerResponse
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Extract issue number from title: "#1", "#15", "001"
        private static readonly Regex IssueNumberRegex = new Regex(
            @"#(\d+)",
            RegexOptions.Compiled);

        // Matches: <article id="post-393579" class="post-393579 ...">
        private static readonly Regex ArticleRegex = new Regex(
            @"<article\s+id=""post-(\d+)""[^>]*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches: <h1 class="post-title"><a href="URL">TITLE</a>
        private static readonly Regex TitleLinkRegex = new Regex(
            @"<h1\s+class=""post-title"">\s*<a\s+href=""([^""]+)""[^>]*>([^<]+)</a>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches: <time datetime="2026-03-27">
        private static readonly Regex DateRegex = new Regex(
            @"<time\s+datetime=""(\d{4}-\d{2}-\d{2})""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Matches file size patterns like "345 MB", "1.2 GB", "45.6 MB"
        private static readonly Regex SizeRegex = new Regex(
            @"(\d+(?:\.\d+)?)\s*(MB|GB|KB)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            var html = indexerResponse.Content;

            if (string.IsNullOrWhiteSpace(html))
            {
                return releases;
            }

            // Split by article tags to process each result
            var articleMatches = ArticleRegex.Matches(html);

            for (var i = 0; i < articleMatches.Count; i++)
            {
                try
                {
                    var articleStart = articleMatches[i].Index;
                    var articleEnd = (i + 1 < articleMatches.Count)
                        ? articleMatches[i + 1].Index
                        : html.Length;

                    var articleHtml = html.Substring(articleStart, articleEnd - articleStart);
                    var postId = articleMatches[i].Groups[1].Value;

                    // Extract title and URL
                    var titleMatch = TitleLinkRegex.Match(articleHtml);
                    if (!titleMatch.Success)
                    {
                        continue;
                    }

                    var infoUrl = titleMatch.Groups[1].Value;
                    var title = DecodeHtmlEntities(titleMatch.Groups[2].Value.Trim());

                    // Extract date
                    var dateMatch = DateRegex.Match(articleHtml);
                    var publishDate = DateTime.UtcNow;
                    if (dateMatch.Success)
                    {
                        if (DateTime.TryParseExact(
                            dateMatch.Groups[1].Value,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal,
                            out var parsedDate))
                        {
                            publishDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        }
                    }

                    // Try to extract size from the article excerpt/body
                    long size = 0;
                    var sizeMatch = SizeRegex.Match(articleHtml);
                    if (sizeMatch.Success)
                    {
                        size = ParseSize(sizeMatch.Groups[1].Value, sizeMatch.Groups[2].Value);
                    }

                    var release = new ReleaseInfo
                    {
                        Guid = $"getcomics-{postId}",
                        Title = title,
                        DownloadUrl = infoUrl,
                        InfoUrl = infoUrl,
                        PublishDate = publishDate,
                        Size = size,
                        Indexer = "GetComics",
                        DownloadProtocol = DownloadProtocol.DirectDownload
                    };

                    releases.Add(release);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to parse GetComics article");
                }
            }

            // Filter/sort by issue number if the search URL contains one
            var requestUrl = indexerResponse.Request?.Url?.ToString() ?? "";
            var searchIssueMatch = IssueNumberRegex.Match(Uri.UnescapeDataString(requestUrl));

            if (searchIssueMatch.Success && int.TryParse(searchIssueMatch.Groups[1].Value, out var requestedIssue))
            {
                // Mylar-style: exact match filtering + relevance sorting
                var exactMatches = new List<ReleaseInfo>();
                var otherResults = new List<ReleaseInfo>();

                foreach (var release in releases)
                {
                    var titleIssueMatch = IssueNumberRegex.Match(release.Title);
                    if (titleIssueMatch.Success && int.TryParse(titleIssueMatch.Groups[1].Value, out var titleIssue))
                    {
                        if (titleIssue == requestedIssue)
                        {
                            exactMatches.Add(release);
                        }
                        else
                        {
                            otherResults.Add(release);
                        }
                    }
                    else
                    {
                        // No issue number in title — could be a TPB or collection, keep it
                        otherResults.Add(release);
                    }
                }

                // Return exact matches first, then others
                exactMatches.AddRange(otherResults);
                return exactMatches;
            }

            return releases;
        }

        private static long ParseSize(string value, string unit)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numericValue))
            {
                return 0;
            }

            return unit.ToUpperInvariant() switch
            {
                "KB" => (long)(numericValue * 1024),
                "MB" => (long)(numericValue * 1024 * 1024),
                "GB" => (long)(numericValue * 1024 * 1024 * 1024),
                _ => 0
            };
        }

        private static string DecodeHtmlEntities(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("&#8211;", "\u2013")
                .Replace("&#8212;", "\u2014")
                .Replace("&#038;", "&")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&#8217;", "\u2019")
                .Replace("&#8216;", "\u2018")
                .Replace("&#8220;", "\u201C")
                .Replace("&#8221;", "\u201D");
        }
    }
}
