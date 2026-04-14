using System.Collections.Generic;
using Panelarr.Api.V1.Issues;

namespace Panelarr.Api.V1.IssueShelf
{
    public class IssueshelfSeriesResource
    {
        public int Id { get; set; }
        public bool? Monitored { get; set; }
        public List<IssueResource> Issues { get; set; }
    }
}
