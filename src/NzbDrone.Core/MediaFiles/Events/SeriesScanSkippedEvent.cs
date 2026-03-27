using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class SeriesScanSkippedEvent : IEvent
    {
        public Series Series { get; private set; }
        public SeriesScanSkippedReason Reason { get; private set; }

        public SeriesScanSkippedEvent(Series author, SeriesScanSkippedReason reason)
        {
            Series = author;
            Reason = reason;
        }
    }

    public enum SeriesScanSkippedReason
    {
        RootFolderDoesNotExist,
        RootFolderIsEmpty
    }
}
