using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderAddedEvent<IDownloadClient>))]
    [CheckOn(typeof(ProviderUpdatedEvent<IDownloadClient>))]
    [CheckOn(typeof(ProviderDeletedEvent<IDownloadClient>))]

    // Two download clients writing into the same directory tree invites
    // conflicts: sync tools prune foreign files, completed-download handling
    // double-processes, and cleanup in one client deletes the other's data.
    // Seen in the wild with a direct-download folder placed inside a
    // seedbox-synced torrent folder.
    public class DownloadClientFolderOverlapCheck : HealthCheckBase, IProvideHealthCheck
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly Logger _logger;

        public DownloadClientFolderOverlapCheck(IProvideDownloadClient downloadClientProvider,
                                                Logger logger,
                                                ILocalizationService localizationService)
            : base(localizationService)
        {
            _downloadClientProvider = downloadClientProvider;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var clients = _downloadClientProvider.GetDownloadClients(true);
            var clientFolders = new List<(string Client, string Folder)>();

            foreach (var client in clients)
            {
                try
                {
                    var folders = client.GetStatus().OutputRootFolders;

                    if (folders == null)
                    {
                        continue;
                    }

                    foreach (var folder in folders)
                    {
                        clientFolders.Add((client.Definition.Name, folder.FullPath));
                    }
                }
                catch (DownloadClientException ex)
                {
                    _logger.Debug(ex, "Unable to communicate with {0} while checking folder overlap", client.Definition.Name);
                }
            }

            foreach (var (client, folder) in clientFolders)
            {
                foreach (var (otherClient, otherFolder) in clientFolders)
                {
                    if (client == otherClient)
                    {
                        continue;
                    }

                    if (folder.PathEquals(otherFolder) || otherFolder.IsParentPath(folder))
                    {
                        return new HealthCheck(GetType(),
                            HealthCheckResult.Warning,
                            string.Format(_localizationService.GetLocalizedString("DownloadClientFolderOverlapHealthCheckMessage"), client, folder, otherClient),
                            "#download-client-folder-overlap");
                    }
                }
            }

            return new HealthCheck(GetType());
        }
    }
}
