using System;
using System.Collections.Generic;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications
{
    public class IssueRetagMessage
    {
        public string Message { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public ComicFile ComicFile { get; set; }
        public Dictionary<string, Tuple<string, string>> Diff { get; set; }
        public bool Scrubbed { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
