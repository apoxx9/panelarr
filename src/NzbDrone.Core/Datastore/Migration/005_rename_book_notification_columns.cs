using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(5)]
    public class RenameBookNotificationColumns : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Column("OnBookDelete").OnTable("Notifications").To("OnIssueDelete");
            Rename.Column("OnBookFileDelete").OnTable("Notifications").To("OnComicFileDelete");
            Rename.Column("OnBookFileDeleteForUpgrade").OnTable("Notifications").To("OnComicFileDeleteForUpgrade");
            Rename.Column("OnBookRetag").OnTable("Notifications").To("OnIssueRetag");
        }
    }
}
