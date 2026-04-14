using System;
using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideIssueInfo
    {
        Tuple<string, Issue, List<SeriesMetadata>> GetIssueInfo(string id);
    }
}
