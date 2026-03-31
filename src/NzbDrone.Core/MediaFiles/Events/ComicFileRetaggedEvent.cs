using System;
using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileRetaggedEvent : IEvent
    {
        public Series Series { get; private set; }
        public ComicFile ComicFile { get; private set; }
        public Dictionary<string, Tuple<string, string>> Diff { get; private set; }
        public bool Scrubbed { get; private set; }

        public ComicFileRetaggedEvent(Series series,
                                      ComicFile comicFile,
                                      Dictionary<string, Tuple<string, string>> diff,
                                      bool scrubbed)
        {
            Series = series;
            ComicFile = comicFile;
            Diff = diff;
            Scrubbed = scrubbed;
        }
    }
}
