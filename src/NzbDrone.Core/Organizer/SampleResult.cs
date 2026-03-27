using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Organizer
{
    public class SampleResult
    {
        public string FileName { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public ComicFile ComicFile { get; set; }
    }
}
