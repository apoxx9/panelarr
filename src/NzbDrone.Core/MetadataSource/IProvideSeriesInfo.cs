using System;
using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideSeriesInfo
    {
        Series GetSeriesInfo(string panelarrId, bool useCache = true);
        HashSet<string> GetChangedSeries(DateTime startTime);
    }
}
