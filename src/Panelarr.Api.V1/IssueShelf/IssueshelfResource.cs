using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace Panelarr.Api.V1.IssueShelf
{
    public class IssueshelfResource
    {
        public List<IssueshelfSeriesResource> Series { get; set; }
        public MonitoringOptions MonitoringOptions { get; set; }
        public NewItemMonitorTypes? MonitorNewItems { get; set; }
    }
}
