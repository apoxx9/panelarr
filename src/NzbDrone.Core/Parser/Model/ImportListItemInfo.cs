using System;

namespace NzbDrone.Core.Parser.Model
{
    public class ImportListItemInfo
    {
        public int ImportListId { get; set; }
        public string ImportList { get; set; }
        public string Series { get; set; }
        public string ForeignSeriesId { get; set; }
        public string Issue { get; set; }
        public string ForeignIssueId { get; set; }
        public string ForeignEditionId { get; set; }
        public DateTime ReleaseDate { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1} [{2}]", ReleaseDate, Series, Issue);
        }
    }
}
