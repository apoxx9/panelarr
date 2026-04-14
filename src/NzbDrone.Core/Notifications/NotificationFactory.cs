using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotificationFactory : IProviderFactory<INotification, NotificationDefinition>
    {
        List<INotification> OnGrabEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnReleaseImportEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnUpgradeEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnRenameEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnHealthIssueEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnSeriesAddedEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnSeriesDeleteEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnIssueDeleteEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnComicFileDeleteEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnComicFileDeleteForUpgradeEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnDownloadFailureEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnImportFailureEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnIssueRetagEnabled(bool filterBlockedNotifications = true);
        List<INotification> OnApplicationUpdateEnabled(bool filterBlockedNotifications = true);
    }

    public class NotificationFactory : ProviderFactory<INotification, NotificationDefinition>, INotificationFactory
    {
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationFactory(INotificationStatusService notificationStatusService, INotificationRepository providerRepository, IEnumerable<INotification> providers, IServiceProvider container, IEventAggregator eventAggregator, Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _notificationStatusService = notificationStatusService;
            _logger = logger;
        }

        protected override List<NotificationDefinition> Active()
        {
            return base.Active().Where(c => c.Enable).ToList();
        }

        public List<INotification> OnGrabEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnGrab).ToList();
        }

        public List<INotification> OnReleaseImportEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnReleaseImport)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnReleaseImport).ToList();
        }

        public List<INotification> OnUpgradeEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnUpgrade)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnUpgrade).ToList();
        }

        public List<INotification> OnRenameEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnRename)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnRename).ToList();
        }

        public List<INotification> OnSeriesAddedEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnSeriesAdded)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnSeriesAdded).ToList();
        }

        public List<INotification> OnSeriesDeleteEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnSeriesDelete)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnSeriesDelete).ToList();
        }

        public List<INotification> OnIssueDeleteEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnIssueDelete)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnIssueDelete).ToList();
        }

        public List<INotification> OnComicFileDeleteEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnComicFileDelete)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnComicFileDelete).ToList();
        }

        public List<INotification> OnComicFileDeleteForUpgradeEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnComicFileDeleteForUpgrade)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnComicFileDeleteForUpgrade).ToList();
        }

        public List<INotification> OnHealthIssueEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnHealthIssue).ToList();
        }

        public List<INotification> OnDownloadFailureEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnDownloadFailure)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnDownloadFailure).ToList();
        }

        public List<INotification> OnImportFailureEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnImportFailure)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnImportFailure).ToList();
        }

        public List<INotification> OnIssueRetagEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnIssueRetag)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnIssueRetag).ToList();
        }

        public List<INotification> OnApplicationUpdateEnabled(bool filterBlockedNotifications = true)
        {
            if (filterBlockedNotifications)
            {
                return FilterBlockedNotifications(GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate)).ToList();
            }

            return GetAvailableProviders().Where(n => ((NotificationDefinition)n.Definition).OnApplicationUpdate).ToList();
        }

        private IEnumerable<INotification> FilterBlockedNotifications(IEnumerable<INotification> notifications)
        {
            var blockedNotifications = _notificationStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var notification in notifications)
            {
                if (blockedNotifications.TryGetValue(notification.Definition.Id, out var notificationStatus))
                {
                    _logger.Debug("Temporarily ignoring notification {0} till {1} due to recent failures.", notification.Definition.Name, notificationStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return notification;
            }
        }

        public override void SetProviderCharacteristics(INotification provider, NotificationDefinition definition)
        {
            base.SetProviderCharacteristics(provider, definition);

            definition.SupportsOnGrab = provider.SupportsOnGrab;
            definition.SupportsOnReleaseImport = provider.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = provider.SupportsOnUpgrade;
            definition.SupportsOnRename = provider.SupportsOnRename;
            definition.SupportsOnSeriesAdded = provider.SupportsOnSeriesAdded;
            definition.SupportsOnSeriesDelete = provider.SupportsOnSeriesDelete;
            definition.SupportsOnIssueDelete = provider.SupportsOnIssueDelete;
            definition.SupportsOnComicFileDelete = provider.SupportsOnComicFileDelete;
            definition.SupportsOnComicFileDeleteForUpgrade = provider.SupportsOnComicFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = provider.SupportsOnHealthIssue;
            definition.SupportsOnDownloadFailure = provider.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = provider.SupportsOnImportFailure;
            definition.SupportsOnIssueRetag = provider.SupportsOnIssueRetag;
            definition.SupportsOnApplicationUpdate = provider.SupportsOnApplicationUpdate;
        }

        public override ValidationResult Test(NotificationDefinition definition)
        {
            var result = base.Test(definition);

            if (definition.Id == 0)
            {
                return result;
            }

            if (result == null || result.IsValid)
            {
                _notificationStatusService.RecordSuccess(definition.Id);
            }
            else
            {
                _notificationStatusService.RecordFailure(definition.Id);
            }

            return result;
        }
    }
}
