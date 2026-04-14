using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace NzbDrone.Core.Indexers.GetComics
{
    public interface IGetComicsDownloadLinkExtractor
    {
        List<GetComicsDownloadLink> ExtractDownloadLinks(string postPageHtml);
    }

    public class GetComicsDownloadLinkExtractor : IGetComicsDownloadLinkExtractor
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Matches anchor tags with href: <a href="URL" ...>LABEL</a>
        private static readonly Regex AnchorRegex = new Regex(
            @"<a\s+[^>]*href=""([^""]+)""[^>]*>(.*?)</a>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // Host detection patterns for direct external links
        private static readonly (Regex Pattern, GetComicsDownloadHost Host)[] DirectHostPatterns =
        {
            (new Regex(@"datanodes\.to", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.DataNodes),
            (new Regex(@"pixeldrain\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Pixeldrain),
            (new Regex(@"vikingfile\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Vikingfile),
            (new Regex(@"fileq\.net", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Fileq),
            (new Regex(@"1024terabox\.com|terabox\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.TeraBox),
            (new Regex(@"rootz\.so", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Rootz),
            (new Regex(@"mega\.nz|megaup\.net", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Mega),
            (new Regex(@"mediafire\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.MediaFire),
            (new Regex(@"drive\.google\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.GoogleDrive),
        };

        // Label-based host detection for getcomics.org/dlds/ redirect links
        private static readonly (Regex Pattern, GetComicsDownloadHost Host)[] LabelHostPatterns =
        {
            (new Regex(@"\bDATANODES?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.DataNodes),
            (new Regex(@"\bPIXELDRAIN\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Pixeldrain),
            (new Regex(@"\bVIKINGFILE\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Vikingfile),
            (new Regex(@"\bFILEQ\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Fileq),
            (new Regex(@"\bTERABOX\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.TeraBox),
            (new Regex(@"\bROOTZ\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Rootz),
            (new Regex(@"\bMEGA\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.Mega),
            (new Regex(@"\bMEDIAFIRE\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.MediaFire),
            (new Regex(@"\bGOOGLE\s*DRIVE\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), GetComicsDownloadHost.GoogleDrive),
        };

        // Priority ordering: prefer direct-downloadable hosts first
        private static readonly Dictionary<GetComicsDownloadHost, int> HostPriority = new Dictionary<GetComicsDownloadHost, int>
        {
            { GetComicsDownloadHost.Pixeldrain, 1 },
            { GetComicsDownloadHost.DataNodes, 2 },
            { GetComicsDownloadHost.Vikingfile, 3 },
            { GetComicsDownloadHost.Fileq, 4 },
            { GetComicsDownloadHost.Rootz, 5 },
            { GetComicsDownloadHost.TeraBox, 6 },
            { GetComicsDownloadHost.Mega, 50 },
            { GetComicsDownloadHost.MediaFire, 51 },
            { GetComicsDownloadHost.GoogleDrive, 52 },
            { GetComicsDownloadHost.Unknown, 99 },
        };

        public List<GetComicsDownloadLink> ExtractDownloadLinks(string postPageHtml)
        {
            var links = new List<GetComicsDownloadLink>();

            if (string.IsNullOrWhiteSpace(postPageHtml))
            {
                return links;
            }

            var anchorMatches = AnchorRegex.Matches(postPageHtml);

            foreach (Match match in anchorMatches)
            {
                var href = match.Groups[1].Value.Trim();
                var label = StripHtmlTags(match.Groups[2].Value).Trim();

                if (string.IsNullOrWhiteSpace(href) || href.StartsWith("#"))
                {
                    continue;
                }

                var isRedirect = href.Contains("getcomics.org/dlds/", StringComparison.OrdinalIgnoreCase);
                var isDirectDownloadLink = IsDownloadHostUrl(href);

                if (!isRedirect && !isDirectDownloadLink)
                {
                    continue;
                }

                var host = DetectHost(href, label, isRedirect);

                links.Add(new GetComicsDownloadLink
                {
                    Url = href,
                    Host = host,
                    Label = label,
                    IsRedirect = isRedirect,
                });
            }

            // Sort by priority (prefer directly downloadable hosts)
            links = links
                .OrderBy(l => HostPriority.GetValueOrDefault(l.Host, 99))
                .ToList();

            Logger.Debug("Extracted {0} download links from GetComics post page", links.Count);

            foreach (var link in links)
            {
                Logger.Trace("  [{0}] {1} (redirect={2}): {3}", link.Host, link.Label, link.IsRedirect, link.Url);
            }

            return links;
        }

        private static bool IsDownloadHostUrl(string url)
        {
            return DirectHostPatterns.Any(p => p.Pattern.IsMatch(url));
        }

        private static GetComicsDownloadHost DetectHost(string url, string label, bool isRedirect)
        {
            // For direct links, detect from the URL itself
            if (!isRedirect)
            {
                foreach (var (pattern, host) in DirectHostPatterns)
                {
                    if (pattern.IsMatch(url))
                    {
                        return host;
                    }
                }
            }

            // For redirect links (getcomics.org/dlds/...), detect from the anchor label
            foreach (var (pattern, host) in LabelHostPatterns)
            {
                if (pattern.IsMatch(label))
                {
                    return host;
                }
            }

            return GetComicsDownloadHost.Unknown;
        }

        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            return Regex.Replace(html, @"<[^>]+>", string.Empty);
        }
    }
}
