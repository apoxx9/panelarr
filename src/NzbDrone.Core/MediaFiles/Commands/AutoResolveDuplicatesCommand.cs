using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    /// <summary>
    /// Command that auto-resolves all duplicate ComicFiles across all issues.
    /// For each issue that has more than one ComicFile, the best-quality file is kept
    /// and all others are moved to the recycle bin and removed from the database.
    ///
    /// Triggered via: POST /api/v1/command  body: { "name": "AutoResolveDuplicates" }
    /// </summary>
    public class AutoResolveDuplicatesCommand : Command
    {
        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
