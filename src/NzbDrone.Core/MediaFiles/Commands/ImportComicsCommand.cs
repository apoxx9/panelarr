using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    /// <summary>
    /// Command that triggers a local comic folder scan and import.
    /// POST /api/v1/command  body: { "name": "ImportComics", "path": "/media/comics" }
    /// </summary>
    public class ImportComicsCommand : Command
    {
        /// <summary>
        /// Path to the folder to scan. If null, all root folders are scanned.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Import mode — Auto, Move, or Copy.
        /// </summary>
        public ImportMode ImportMode { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
