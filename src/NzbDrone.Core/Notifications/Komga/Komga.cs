using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Komga
{
    public class Komga : NotificationBase<KomgaSettings>
    {
        private readonly IKomgaService _komgaService;
        private readonly Logger _logger;

        public Komga(IKomgaService komgaService, Logger logger)
        {
            _komgaService = komgaService;
            _logger = logger;
        }

        public override string Name => "Komga";

        public override string Link => "https://komga.org/";

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            if (!Settings.Notify)
            {
                return;
            }

            _logger.Debug("Triggering Komga library scan after import of {0}", message.Issue?.Title);
            _komgaService.TriggerLibraryScan(Settings);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            if (!Settings.Notify)
            {
                return;
            }

            _komgaService.TriggerLibraryScan(Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_komgaService.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
