using System;
using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideBookInfo
    {
        Tuple<string, Issue, List<SeriesMetadata>> GetBookInfo(string id);
    }
}
