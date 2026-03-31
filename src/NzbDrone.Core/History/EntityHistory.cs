using System;
using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public class EntityHistory : ModelBase
    {
        public const string DOWNLOAD_CLIENT = "downloadClient";
        public const string RELEASE_SOURCE = "releaseSource";
        public const string RELEASE_GROUP = "releaseGroup";
        public const string SIZE = "size";
        public const string INDEXER = "indexer";

        public EntityHistory()
        {
            Data = new Dictionary<string, string>();
        }

        public int IssueId { get; set; }
        public int SeriesId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public DateTime Date { get; set; }
        public Issue Issue { get; set; }
        public Series Series { get; set; }
        public EntityHistoryEventType EventType { get; set; }
        public Dictionary<string, string> Data { get; set; }

        public string DownloadId { get; set; }
    }

    public enum EntityHistoryEventType
    {
        Unknown = 0,
        Grabbed = 1,
        ComicFileImported = 3,
        DownloadFailed = 4,
        ComicFileDeleted = 5,
        ComicFileRenamed = 6,
        IssueImportIncomplete = 7,
        DownloadImported = 8,
        ComicFileRetagged = 9,
        DownloadIgnored = 10
    }
}
