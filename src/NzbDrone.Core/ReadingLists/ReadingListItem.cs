using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.ReadingLists
{
    // One ordered slot of an arc. The library link (IssueId) is derived and
    // nullable: slots keep their identity (foreign id + display fields) when
    // library content comes and goes — deleting a series UNRESOLVES its
    // slots, it never deletes them. Have/missing is computed live from the
    // IssueId join, never stored (Mylar persists status and it drifts).
    public class ReadingListItem : ModelBase
    {
        public int ReadingListId { get; set; }
        public int Position { get; set; }

        // Resolved link to a library issue; null when the issue (or its
        // series) is not in the library.
        public int? IssueId { get; set; }

        // cv:<issue id> when known (provider imports and id-carrying CBLs).
        public string ForeignIssueId { get; set; }

        // Display + fallback-matching fields, CBL Book attributes:
        // Volume is the series start year by community convention.
        public string SeriesName { get; set; }
        public string IssueNumber { get; set; }
        public string Volume { get; set; }
        public string Year { get; set; }
    }
}
