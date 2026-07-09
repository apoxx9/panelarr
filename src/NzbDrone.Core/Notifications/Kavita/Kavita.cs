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
        NotifyScan(Directory.GetParent(allPaths.First())?.FullName);
    }

    public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
    {
        var allPaths = deleteMessage.Issue.ComicFiles.Value.Select(v => v.Path).Distinct();
        NotifyScan(Directory.GetParent(allPaths.First())?.FullName);
    }

    public override void OnComicFileDelete(ComicFileDeleteMessage message)
    {
        NotifyScan(Directory.GetParent(message.ComicFile.Path)?.FullName);
    }

    public override void OnIssueRetag(IssueRetagMessage message)
    {
        NotifyScan(Directory.GetParent(message.ComicFile.Path)?.FullName);
    }

    public override string Name => "Kavita";

    public override ValidationResult Test()
    {
        var failures = new List<ValidationFailure>();

        failures.AddIfNotNull(_kavitaService.Test(Settings, "Success! Kavita has been successfully configured!"));

        return new ValidationResult(failures);
    }

    // Kavita's scan-folder endpoint takes a bare library folder path — never
    // prefix it with a notification title (Kavita 500s on unmatchable paths)
    private void NotifyScan(string folderPath)
    {
        if (folderPath == null || !Settings.Notify)
        {
            return;
        }

        try
        {
            _kavitaService.Notify(Settings, folderPath);
        }
        catch (SocketException ex)
        {
            _logger.Debug(ex, "Unable to connect to Kavita Host: {0}:{1}", Settings.Host, Settings.Port);
        }
    }
}
