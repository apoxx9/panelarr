using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    // In-memory only: reports exist so the import modal can show per-file
    // results right after the command it queued finishes; they are not
    // history and do not survive a restart.
    public interface IStagingImportReportService
    {
        void Store(StagingImportReport report);
        StagingImportReport Find(int commandId);
    }

    public class StagingImportReportService : IStagingImportReportService
    {
        private const int MaxReports = 5;

        private readonly object _mutex = new object();
        private readonly List<StagingImportReport> _reports = new List<StagingImportReport>();

        public void Store(StagingImportReport report)
        {
            lock (_mutex)
            {
                _reports.RemoveAll(r => r.CommandId == report.CommandId);
                _reports.Insert(0, report);

                if (_reports.Count > MaxReports)
                {
                    _reports.RemoveRange(MaxReports, _reports.Count - MaxReports);
                }
            }
        }

        public StagingImportReport Find(int commandId)
        {
            lock (_mutex)
            {
                return _reports.FirstOrDefault(r => r.CommandId == commandId);
            }
        }
    }
}
