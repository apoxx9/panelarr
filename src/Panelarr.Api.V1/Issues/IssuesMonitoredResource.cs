using System.Collections.Generic;

namespace Panelarr.Api.V1.Books
{
    public class IssuesMonitoredResource
    {
        public List<int> IssueIds { get; set; }
        public bool Monitored { get; set; }
    }
}
