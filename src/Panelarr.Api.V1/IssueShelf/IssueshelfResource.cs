using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace Panelarr.Api.V1.Bookshelf
{
    public class IssueshelfResource
    {
        public List<IssueshelfSeriesResource> Seriess { get; set; }
        public MonitoringOptions MonitoringOptions { get; set; }
        public NewItemMonitorTypes? MonitorNewItems { get; set; }
    }
}
