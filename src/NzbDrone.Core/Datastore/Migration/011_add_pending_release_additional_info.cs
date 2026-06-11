using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // PendingRelease.AdditionalInfo (release-source tracking) was never
    // persisted: the column was missing, so the value silently dropped on
    // restart and reloaded releases always reported ReleaseSourceType.Unknown.
    [Migration(11)]
    public class AddPendingReleaseAdditionalInfo : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("PendingReleases").AddColumn("AdditionalInfo").AsString().Nullable();
        }
    }
}
