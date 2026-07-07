using System.Collections.Generic;

namespace NzbDrone.Core.Notifications
{
    public class ReaderPushResult
    {
        public bool Updated { get; set; }
        public int MatchedCount { get; set; }
        public List<string> Unmatched { get; set; } = new List<string>();
    }
}
