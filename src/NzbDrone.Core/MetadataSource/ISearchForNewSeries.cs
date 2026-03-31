using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewSeries
    {
        List<Series> SearchForNewSeries(string title);
    }
}
