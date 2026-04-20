using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewIssue
    {
        List<Issue> SearchForNewIssue(string title, string series, bool getAllEditions = true);
    }
}
