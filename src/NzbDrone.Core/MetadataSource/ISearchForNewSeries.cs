using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewSeries
    {
        List<Series> SearchForNewSeries(string title);
    }
}
