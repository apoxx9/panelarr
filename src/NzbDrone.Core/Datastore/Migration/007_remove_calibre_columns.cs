using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(7)]
    public class RemoveCalibreColumns : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("IsCalibreLibrary").FromTable("RootFolders");
            Delete.Column("CalibreSettings").FromTable("RootFolders");
        }
    }
}
