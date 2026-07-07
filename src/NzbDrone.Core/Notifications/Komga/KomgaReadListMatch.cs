using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Notifications.Komga
{
    // Wire DTOs for Komga's readlist endpoints (match/comicrack + readlists).
    public class KomgaReadListMatch
    {
        [JsonPropertyName("readListMatch")]
        public KomgaReadListMatchInfo ReadListMatch { get; set; }

        [JsonPropertyName("requests")]
        public List<KomgaReadListMatchRequest> Requests { get; set; } = new List<KomgaReadListMatchRequest>();

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }
    }

    public class KomgaReadListMatchInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; }
    }

    public class KomgaReadListMatchRequest
    {
        [JsonPropertyName("request")]
        public KomgaReadListMatchRequestBook Request { get; set; }

        [JsonPropertyName("matches")]
        public List<KomgaReadListSeriesMatch> Matches { get; set; } = new List<KomgaReadListSeriesMatch>();
    }

    public class KomgaReadListMatchRequestBook
    {
        [JsonPropertyName("series")]
        public List<string> Series { get; set; } = new List<string>();

        [JsonPropertyName("number")]
        public string Number { get; set; }
    }

    public class KomgaReadListSeriesMatch
    {
        [JsonPropertyName("series")]
        public KomgaSeriesInfo Series { get; set; }

        [JsonPropertyName("books")]
        public List<KomgaBookInfo> Books { get; set; } = new List<KomgaBookInfo>();
    }

    public class KomgaSeriesInfo
    {
        [JsonPropertyName("seriesId")]
        public string SeriesId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class KomgaBookInfo
    {
        [JsonPropertyName("bookId")]
        public string BookId { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class KomgaReadListPage
    {
        [JsonPropertyName("content")]
        public List<KomgaReadList> Content { get; set; } = new List<KomgaReadList>();
    }

    public class KomgaReadList
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
