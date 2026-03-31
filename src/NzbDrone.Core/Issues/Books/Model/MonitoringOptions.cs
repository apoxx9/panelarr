using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues
{
    public class MonitoringOptions : IEmbeddedDocument
    {
        public MonitoringOptions()
        {
            IssuesToMonitor = new List<string>();
        }

        public MonitorTypes Monitor { get; set; }
        public List<string> IssuesToMonitor { get; set; }
        public bool Monitored { get; set; }
    }
}
