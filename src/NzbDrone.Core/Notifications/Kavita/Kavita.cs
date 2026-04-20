using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.Kavita;

public class Kavita : NotificationBase<KavitaSettings>
{
    private readonly IKavitaService _kavitaService;
    private readonly Logger _logger;

    public Kavita(IKavitaService kavitaService, Logger logger)
    {
        _kavitaService = kavitaService;
        _logger = logger;
    }

    public override string Link => "https://www.kavitareader.com/";

    public override void OnReleaseImport(IssueDownloadMessage message)
    {
        var allPaths = message.ComicFiles.Select(v => v.Path).Distinct();
        var path = Directory.GetParent(allPaths.First())?.FullName;
        Notify(Settings, ISSUE_DOWNLOADED_TITLE_BRANDED, path);
    }

    public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
    {
        var allPaths = deleteMessage.Issue.ComicFiles.Value.Select(v => v.Path).Distinct();
        var path = Directory.GetParent(allPaths.First())?.FullName;
        Notify(Settings, ISSUE_FILE_DELETED_TITLE_BRANDED, path);
    }

    public override void OnComicFileDelete(ComicFileDeleteMessage message)
    {
        Notify(Settings, ISSUE_FILE_DELETED_TITLE_BRANDED, Directory.GetParent(message.ComicFile.Path)?.FullName);
    }

    public override void OnIssueRetag(IssueRetagMessage message)
    {
        Notify(Settings, ISSUE_RETAGGED_TITLE_BRANDED, Directory.GetParent(message.ComicFile.Path)?.FullName);
    }

    public override string Name => "Kavita";

    public override ValidationResult Test()
    {
        var failures = new List<ValidationFailure>();

        failures.AddIfNotNull(_kavitaService.Test(Settings, "Success! Kavita has been successfully configured!"));

        return new ValidationResult(failures);
    }

    private void Notify(KavitaSettings settings, string header, string message)
    {
        try
        {
            if (Settings.Notify)
            {
                _kavitaService.Notify(Settings, $"{header} - {message}");
            }
        }
        catch (SocketException ex)
        {
            var logMessage = $"Unable to connect to Subsonic Host: {Settings.Host}:{Settings.Port}";
            _logger.Debug(ex, logMessage);
        }
    }
}
