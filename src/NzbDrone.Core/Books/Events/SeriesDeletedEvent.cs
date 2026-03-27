using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class SeriesDeletedEvent : IEvent
    {
        public Series Series { get; private set; }
        public bool DeleteFiles { get; private set; }
        public bool AddImportListExclusion { get; private set; }

        public SeriesDeletedEvent(Series author, bool deleteFiles, bool addImportListExclusion)
        {
            Series = author;
            DeleteFiles = deleteFiles;
            AddImportListExclusion = addImportListExclusion;
        }
    }
}
