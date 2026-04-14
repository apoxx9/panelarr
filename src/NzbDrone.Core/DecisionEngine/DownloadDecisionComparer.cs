using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecisionComparer : IComparer<DownloadDecision>
    {
        private readonly IConfigService _configService;
        private readonly IDelayProfileService _delayProfileService;

        public delegate int CompareDelegate(DownloadDecision x, DownloadDecision y);
        public delegate int CompareDelegate<TSubject, TValue>(DownloadDecision x, DownloadDecision y);

        public DownloadDecisionComparer(IConfigService configService, IDelayProfileService delayProfileService)
        {
            _configService = configService;
            _delayProfileService = delayProfileService;
        }

        public int Compare(DownloadDecision x, DownloadDecision y)
        {
            var comparers = new List<CompareDelegate>
            {
                CompareQuality,
                CompareCustomFormatScore,
                CompareProtocol,
                CompareIndexerPriority,
                ComparePeersIfTorrent,
                CompareIssueCount,
                CompareAgeIfUsenet,
                CompareSize
            };

            return comparers.Select(comparer => comparer(x, y)).FirstOrDefault(result => result != 0);
        }

        private int CompareBy<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            var leftValue = funcValue(left);
            var rightValue = funcValue(right);

            return leftValue.CompareTo(rightValue);
        }

        private int CompareByReverse<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            return CompareBy(left, right, funcValue) * -1;
        }

        private int CompareAll(params int[] comparers)
        {
            return comparers.Select(comparer => comparer).FirstOrDefault(result => result != 0);
        }

        private int CompareIndexerPriority(DownloadDecision x, DownloadDecision y)
        {
            return CompareByReverse(x.RemoteIssue.Release, y.RemoteIssue.Release, release => release.IndexerPriority);
        }

        private int CompareQuality(DownloadDecision x, DownloadDecision y)
        {
            if (_configService.DownloadPropersAndRepacks == ProperDownloadTypes.DoNotPrefer)
            {
                return CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.Series.QualityProfile.Value.GetIndex(remoteIssue.ParsedIssueInfo.Quality.Quality));
            }

            return CompareAll(CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.Series.QualityProfile.Value.GetIndex(remoteIssue.ParsedIssueInfo.Quality.Quality)),
                           CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.ParsedIssueInfo.Quality.Revision));
        }

        private int CompareCustomFormatScore(DownloadDecision x, DownloadDecision y)
        {
            return CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.CustomFormatScore);
        }

        private int CompareProtocol(DownloadDecision x, DownloadDecision y)
        {
            var result = CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue =>
            {
                var delayProfile = _delayProfileService.BestForTags(remoteIssue.Series.Tags);
                var downloadProtocol = remoteIssue.Release.DownloadProtocol;
                return downloadProtocol == delayProfile.PreferredProtocol;
            });

            return result;
        }

        private int CompareIssueCount(DownloadDecision x, DownloadDecision y)
        {
            var discographyCompare = CompareBy(x.RemoteIssue,
                y.RemoteIssue,
                remoteIssue => remoteIssue.ParsedIssueInfo.Discography);

            if (discographyCompare != 0)
            {
                return discographyCompare;
            }

            return CompareByReverse(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.Issues.Count);
        }

        private int ComparePeersIfTorrent(DownloadDecision x, DownloadDecision y)
        {
            // Different protocols should get caught when checking the preferred protocol,
            // since we're dealing with the same series in our comparisions
            if (x.RemoteIssue.Release.DownloadProtocol != DownloadProtocol.Torrent ||
                y.RemoteIssue.Release.DownloadProtocol != DownloadProtocol.Torrent)
            {
                return 0;
            }

            return CompareAll(
                CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue =>
                {
                    var seeders = TorrentInfo.GetSeeders(remoteIssue.Release);

                    return seeders.HasValue && seeders.Value > 0 ? Math.Round(Math.Log10(seeders.Value)) : 0;
                }),
                CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue =>
                {
                    var peers = TorrentInfo.GetPeers(remoteIssue.Release);

                    return peers.HasValue && peers.Value > 0 ? Math.Round(Math.Log10(peers.Value)) : 0;
                }));
        }

        private int CompareAgeIfUsenet(DownloadDecision x, DownloadDecision y)
        {
            if (x.RemoteIssue.Release.DownloadProtocol != DownloadProtocol.Usenet ||
                y.RemoteIssue.Release.DownloadProtocol != DownloadProtocol.Usenet)
            {
                return 0;
            }

            return CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue =>
            {
                var ageHours = remoteIssue.Release.AgeHours;
                var age = remoteIssue.Release.Age;

                if (ageHours < 1)
                {
                    return 1000;
                }

                if (ageHours <= 24)
                {
                    return 100;
                }

                if (age <= 7)
                {
                    return 10;
                }

                return 1;
            });
        }

        private int CompareSize(DownloadDecision x, DownloadDecision y)
        {
            // TODO: Is smaller better? Smaller for usenet could mean no par2 files.
            return CompareBy(x.RemoteIssue, y.RemoteIssue, remoteIssue => remoteIssue.Release.Size.Round(200.Megabytes()));
        }
    }
}
