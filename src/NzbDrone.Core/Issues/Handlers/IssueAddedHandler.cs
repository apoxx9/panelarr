using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class IssueAddedHandler : IHandle<IssueAddedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public IssueAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(IssueAddedEvent message)
        {
            if (message.DoRefresh)
            {
                _commandQueueManager.Push(new RefreshSeriesCommand(message.Issue.Series.Value.Id));
            }
        }
    }
}
