using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class SeriesAddedHandler : IHandle<SeriesAddedEvent>,
                                      IHandle<SeriesImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(SeriesAddedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new RefreshSeriesCommand(message.Series.Id, true));
            }
        }

        public void Handle(SeriesImportedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new BulkRefreshSeriesCommand(message.SeriesIds, true));
            }
        }
    }
}
