using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.CustomScript
{
    public class CustomScript : NotificationBase<CustomScriptSettings>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IProcessProvider _processProvider;
        private readonly Logger _logger;

        public CustomScript(IDiskProvider diskProvider, IProcessProvider processProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _processProvider = processProvider;
            _logger = logger;
        }

        public override string Name => "Custom Script";

        public override string Link => "https://wiki.servarr.com/panelarr/settings#connections";

        public override ProviderMessage Message => new ProviderMessage("Testing will execute the script with the EventType set to Test, ensure your script handles this correctly", ProviderMessageType.Warning);

        public override void OnGrab(GrabMessage message)
        {
            var series = message.Series;
            var remoteIssue = message.RemoteIssue;
            var releaseGroup = remoteIssue.ParsedIssueInfo.ReleaseGroup;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "Grab");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Metadata.Value.Name);
            environmentVariables.Add("Panelarr_Series_GRId", series.Metadata.Value.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Release_IssueCount", remoteIssue.Issues.Count.ToString());
            environmentVariables.Add("Panelarr_Release_IssueReleaseDates", string.Join(",", remoteIssue.Issues.Select(e => e.ReleaseDate)));
            environmentVariables.Add("Panelarr_Release_IssueTitles", string.Join("|", remoteIssue.Issues.Select(e => e.Title)));
            environmentVariables.Add("Panelarr_Release_IssueIds", string.Join("|", remoteIssue.Issues.Select(e => e.Id.ToString())));
            environmentVariables.Add("Panelarr_Release_GRIds", remoteIssue.Issues.Select(x => x.ForeignIssueId).ConcatToString("|"));
            environmentVariables.Add("Panelarr_Release_Title", remoteIssue.Release.Title);
            environmentVariables.Add("Panelarr_Release_Indexer", remoteIssue.Release.Indexer ?? string.Empty);
            environmentVariables.Add("Panelarr_Release_Size", remoteIssue.Release.Size.ToString());
            environmentVariables.Add("Panelarr_Release_Quality", remoteIssue.ParsedIssueInfo.Quality.Quality.Name);
            environmentVariables.Add("Panelarr_Release_QualityVersion", remoteIssue.ParsedIssueInfo.Quality.Revision.Version.ToString());
            environmentVariables.Add("Panelarr_Release_ReleaseGroup", releaseGroup ?? string.Empty);
            environmentVariables.Add("Panelarr_Release_IndexerFlags", remoteIssue.Release.IndexerFlags.ToString());
            environmentVariables.Add("Panelarr_Download_Client", message.DownloadClientName ?? string.Empty);
            environmentVariables.Add("Panelarr_Download_Client_Type", message.DownloadClientType ?? string.Empty);
            environmentVariables.Add("Panelarr_Download_Id", message.DownloadId ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            var series = message.Series;
            var issue = message.Issue;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "Download");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Metadata.Value.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_GRId", series.Metadata.Value.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Issue_Id", issue.Id.ToString());
            environmentVariables.Add("Panelarr_Issue_Title", issue.Title);
            environmentVariables.Add("Panelarr_Issue_GRId", issue.ForeignIssueId);
            environmentVariables.Add("Panelarr_Issue_ReleaseDate", issue.ReleaseDate.ToString());
            environmentVariables.Add("Panelarr_Download_Client", message.DownloadClientInfo?.Name ?? string.Empty);
            environmentVariables.Add("Panelarr_Download_Client_Type", message.DownloadClientInfo?.Type ?? string.Empty);
            environmentVariables.Add("Panelarr_Download_Id", message.DownloadId ?? string.Empty);

            if (message.ComicFiles.Any())
            {
                environmentVariables.Add("Panelarr_AddedIssuePaths", string.Join("|", message.ComicFiles.Select(e => e.Path)));
            }

            if (message.OldFiles.Any())
            {
                environmentVariables.Add("Panelarr_DeletedPaths", string.Join("|", message.OldFiles.Select(e => e.Path)));
                environmentVariables.Add("Panelarr_DeletedDateAdded", string.Join("|", message.OldFiles.Select(e => e.DateAdded)));
            }

            ExecuteScript(environmentVariables);
        }

        public override void OnRename(Series series, List<RenamedComicFile> renamedFiles)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "Rename");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Metadata.Value.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_GRId", series.Metadata.Value.ForeignSeriesId);

            ExecuteScript(environmentVariables);
        }

        public override void OnSeriesAdded(Series series)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "SeriesAdded");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Metadata.Value.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_GRId", series.Metadata.Value.ForeignSeriesId);

            ExecuteScript(environmentVariables);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            var series = deleteMessage.Series;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "SeriesDelete");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_ForeignId", series.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Series_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            var series = deleteMessage.Issue.Series.Value;
            var issue = deleteMessage.Issue;

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "IssueDelete");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_ForeignId", series.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Issue_Id", issue.Id.ToString());
            environmentVariables.Add("Panelarr_Issue_Title", issue.Title);
            environmentVariables.Add("Panelarr_Issue_ForeignId", issue.ForeignIssueId);
            environmentVariables.Add("Panelarr_Issue_DeletedFiles", deleteMessage.DeletedFiles.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            var series = deleteMessage.Issue.Series.Value;
            var issue = deleteMessage.Issue;
            var comicFile = deleteMessage.ComicFile;

            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "ComicFileDelete");
            environmentVariables.Add("Panelarr_Delete_Reason", deleteMessage.Reason.ToString());
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Name);
            environmentVariables.Add("Panelarr_Series_ForeignId", series.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Issue_Id", issue.Id.ToString());
            environmentVariables.Add("Panelarr_Issue_Title", issue.Title);
            environmentVariables.Add("Panelarr_Issue_ForeignId", issue.ForeignIssueId);
            environmentVariables.Add("Panelarr_ComicFile_Id", comicFile.Id.ToString());
            environmentVariables.Add("Panelarr_ComicFile_Path", comicFile.Path);
            environmentVariables.Add("Panelarr_ComicFile_Quality", comicFile.Quality.Quality.Name);
            environmentVariables.Add("Panelarr_ComicFile_QualityVersion", comicFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Panelarr_ComicFile_ReleaseGroup", comicFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Panelarr_ComicFile_SceneName", comicFile.SceneName ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnIssueRetag(IssueRetagMessage message)
        {
            var series = message.Series;
            var issue = message.Issue;
            var comicFile = message.ComicFile;
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "TrackRetag");
            environmentVariables.Add("Panelarr_Series_Id", series.Id.ToString());
            environmentVariables.Add("Panelarr_Series_Name", series.Metadata.Value.Name);
            environmentVariables.Add("Panelarr_Series_Path", series.Path);
            environmentVariables.Add("Panelarr_Series_GRId", series.Metadata.Value.ForeignSeriesId);
            environmentVariables.Add("Panelarr_Issue_Id", issue.Id.ToString());
            environmentVariables.Add("Panelarr_Issue_Title", issue.Title);
            environmentVariables.Add("Panelarr_Issue_GRId", issue.ForeignIssueId);
            environmentVariables.Add("Panelarr_Issue_ReleaseDate", issue.ReleaseDate.ToString());
            environmentVariables.Add("Panelarr_ComicFile_Id", comicFile.Id.ToString());
            environmentVariables.Add("Panelarr_ComicFile_Path", comicFile.Path);
            environmentVariables.Add("Panelarr_ComicFile_Quality", comicFile.Quality.Quality.Name);
            environmentVariables.Add("Panelarr_ComicFile_QualityVersion", comicFile.Quality.Revision.Version.ToString());
            environmentVariables.Add("Panelarr_ComicFile_ReleaseGroup", comicFile.ReleaseGroup ?? string.Empty);
            environmentVariables.Add("Panelarr_ComicFile_SceneName", comicFile.SceneName ?? string.Empty);
            environmentVariables.Add("Panelarr_Tags_Diff", message.Diff.ToJson());
            environmentVariables.Add("Panelarr_Tags_Scrubbed", message.Scrubbed.ToString());

            ExecuteScript(environmentVariables);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "HealthIssue");
            environmentVariables.Add("Panelarr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
            environmentVariables.Add("Panelarr_Health_Issue_Message", healthCheck.Message);
            environmentVariables.Add("Panelarr_Health_Issue_Type", healthCheck.Source.Name);
            environmentVariables.Add("Panelarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

            ExecuteScript(environmentVariables);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var environmentVariables = new StringDictionary();

            environmentVariables.Add("Panelarr_EventType", "ApplicationUpdate");
            environmentVariables.Add("Panelarr_Update_Message", updateMessage.Message);
            environmentVariables.Add("Panelarr_Update_NewVersion", updateMessage.NewVersion.ToString());
            environmentVariables.Add("Panelarr_Update_PreviousVersion", updateMessage.PreviousVersion.ToString());

            ExecuteScript(environmentVariables);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            if (!_diskProvider.FileExists(Settings.Path))
            {
                failures.Add(new NzbDroneValidationFailure("Path", "File does not exist"));
            }

            if (failures.Empty())
            {
                try
                {
                    var environmentVariables = new StringDictionary();
                    environmentVariables.Add("Panelarr_EventType", "Test");

                    var processOutput = ExecuteScript(environmentVariables);

                    if (processOutput.ExitCode != 0)
                    {
                        failures.Add(new NzbDroneValidationFailure(string.Empty, $"Script exited with code: {processOutput.ExitCode}"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    failures.Add(new NzbDroneValidationFailure(string.Empty, ex.Message));
                }
            }

            return new ValidationResult(failures);
        }

        private ProcessOutput ExecuteScript(StringDictionary environmentVariables)
        {
            _logger.Debug("Executing external script: {0}", Settings.Path);

            var processOutput = _processProvider.StartAndCapture(Settings.Path, Settings.Arguments, environmentVariables);

            _logger.Debug("Executed external script: {0} - Status: {1}", Settings.Path, processOutput.ExitCode);
            _logger.Debug($"Script Output: {System.Environment.NewLine}{string.Join(System.Environment.NewLine, processOutput.Lines)}");

            return processOutput;
        }
    }
}
