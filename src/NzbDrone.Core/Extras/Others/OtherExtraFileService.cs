using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Others
{
    public interface IOtherExtraFileService : IExtraFileService<OtherExtraFile>
    {
    }

    public class OtherExtraFileService : ExtraFileService<OtherExtraFile>, IOtherExtraFileService
    {
        public OtherExtraFileService(IExtraFileRepository<OtherExtraFile> repository, ISeriesService authorService, IDiskProvider diskProvider, IRecycleBinProvider recycleBinProvider, Logger logger)
            : base(repository, authorService, diskProvider, recycleBinProvider, logger)
        {
        }
    }
}
