using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Parser.Model
{
    public class RemoteIssue
    {
        public ReleaseInfo Release { get; set; }
        public ParsedIssueInfo ParsedIssueInfo { get; set; }
        public Series Series { get; set; }
        public List<Issue> Issues { get; set; }
        public bool DownloadAllowed { get; set; }
        public TorrentSeedConfiguration SeedConfiguration { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public ReleaseSourceType ReleaseSource { get; set; }

        public RemoteIssue()
        {
            Issues = new List<Issue>();
            CustomFormats = new List<CustomFormat>();
        }

        public bool IsRecentIssue()
        {
            return Issues.Any(e => e.ReleaseDate >= DateTime.UtcNow.Date.AddDays(-14));
        }

        public override string ToString()
        {
            return Release.Title;
        }
    }

    public enum ReleaseSourceType
    {
        Unknown = 0,
        Rss = 1,
        Search = 2,
        UserInvokedSearch = 3,
        InteractiveSearch = 4,
        ReleasePush = 5
    }
}
