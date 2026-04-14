using System.Collections.Generic;

namespace Panelarr.Api.V1.Issues
{
    public class IssueEditorResource
    {
        public List<int> IssueIds { get; set; }
        public bool? Monitored { get; set; }
        public bool? DeleteFiles { get; set; }
        public bool? AddImportListExclusion { get; set; }
    }
}
