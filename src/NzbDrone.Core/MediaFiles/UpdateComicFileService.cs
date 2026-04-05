using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpdateComicFileService
    {
        void ChangeFileDateForFile(ComicFile comicFile, Series series, Issue issue);
    }

    public class UpdateComicFileService : IUpdateComicFileService,
                                            IHandle<SeriesScannedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IIssueService _issueService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;
        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public UpdateComicFileService(IDiskProvider diskProvider,
                                      IConfigService configService,
                                      IIssueService issueService,
                                      Logger logger)
        {
            _diskProvider = diskProvider;
            _configService = configService;
            _issueService = issueService;
            _logger = logger;
        }

        public void ChangeFileDateForFile(ComicFile comicFile, Series series, Issue issue)
        {
            ChangeFileDate(comicFile, issue);
        }

        private bool ChangeFileDate(ComicFile comicFile, Issue issue)
        {
            var comicFilePath = comicFile.Path;

            switch (_configService.FileDate)
            {
                case FileDateType.IssueReleaseDate:
                    {
                        if (!issue.ReleaseDate.HasValue)
                        {
                            _logger.Debug("Could not create valid date to change file [{0}]", comicFilePath);
                            return false;
                        }

                        var relDate = issue.ReleaseDate.Value;

                        // avoiding false +ve checks and set date skewing by not using UTC (Windows)
                        var oldDateTime = _diskProvider.FileGetLastWrite(comicFilePath);

                        if (OsInfo.IsNotWindows && relDate < EpochTime)
                        {
                            _logger.Debug("Setting date of file to 1970-01-01 as actual airdate is before that time and will not be set properly");
                            relDate = EpochTime;
                        }

                        if (!DateTime.Equals(relDate, oldDateTime))
                        {
                            try
                            {
                                _diskProvider.FileSetLastWriteTime(comicFilePath, relDate);
                                _logger.Debug("Date of file [{0}] changed from '{1}' to '{2}'", comicFilePath, oldDateTime, relDate);

                                return true;
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to set date of file [" + comicFilePath + "]");
                            }
                        }

                        return false;
                    }
            }

            return false;
        }

        public void Handle(SeriesScannedEvent message)
        {
            if (_configService.FileDate == FileDateType.None)
            {
                return;
            }

            var issues = _issueService.GetSeriesIssuesWithFiles(message.Series);

            var comicFiles = new List<ComicFile>();
            var updated = new List<ComicFile>();

            foreach (var issue in issues)
            {
                var files = issue.ComicFiles.Value;
                foreach (var file in files)
                {
                    comicFiles.Add(file);
                    if (ChangeFileDate(file, issue))
                    {
                        updated.Add(file);
                    }
                }
            }

            if (updated.Any())
            {
                _logger.ProgressDebug("Changed file date for {0} files of {1} in {2}", updated.Count, comicFiles.Count, message.Series.Name);
            }
            else
            {
                _logger.ProgressDebug("No file dates changed for {0}", message.Series.Name);
            }
        }
    }
}
