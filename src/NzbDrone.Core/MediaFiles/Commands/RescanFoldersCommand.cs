using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RescanFoldersCommand : Command
    {
        public RescanFoldersCommand()
        {
            // These are the settings used in the scheduled task
            Filter = FilterFilesType.Known;
            AddNewSeries = true;
        }

        public RescanFoldersCommand(List<string> folders, FilterFilesType filter, bool addNewSeriess, List<int> authorIds)
        {
            Folders = folders;
            Filter = filter;
            AddNewSeries = addNewSeriess;
            SeriesIds = authorIds;
        }

        public List<string> Folders { get; set; }
        public FilterFilesType Filter { get; set; }
        public bool AddNewSeries { get; set; }
        public List<int> SeriesIds { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
