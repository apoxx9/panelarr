namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public interface IAggregate<T>
    {
        T Aggregate(T item, bool otherFiles);
    }
}
