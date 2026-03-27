using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class SeriesMovedEvent : IEvent
    {
        public Series Series { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public SeriesMovedEvent(Series author, string sourcePath, string destinationPath)
        {
            Series = author;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
