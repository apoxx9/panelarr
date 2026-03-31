using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(5)]
    public class RenameIssueNotificationColumns : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Columns were already created with correct names in 001_initial_schema.
            // This migration is retained for schema version tracking only.
        }
    }
}
