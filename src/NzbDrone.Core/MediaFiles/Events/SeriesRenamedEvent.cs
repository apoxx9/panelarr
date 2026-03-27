using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class SeriesRenamedEvent : IEvent
    {
        public Series Series { get; private set; }
        public List<RenamedComicFile> RenamedFiles { get; private set; }

        public SeriesRenamedEvent(Series author, List<RenamedComicFile> renamedFiles)
        {
            Series = author;
            RenamedFiles = renamedFiles;
        }
    }
}
