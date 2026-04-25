using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace Panelarr.Api.V1.ComicFiles
{
    public class ComicFileListResource
    {
        public List<int> ComicFileIds { get; set; }
        public QualityModel Quality { get; set; }
    }
}
