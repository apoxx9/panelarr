using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation.Aggregators
{
    public interface IAggregateRemoteIssue
    {
        RemoteIssue Aggregate(RemoteIssue remoteIssue);
    }
}
