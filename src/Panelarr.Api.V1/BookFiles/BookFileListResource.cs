using System.Collections.Generic;
using NzbDrone.Core.Qualities;

namespace Panelarr.Api.V1.BookFiles
{
    public class BookFileListResource
    {
        public List<int> BookFileIds { get; set; }
        public QualityModel Quality { get; set; }
    }
}
