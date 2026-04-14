using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(3)]
    public class AutoUnmonitorAfterDownload : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series")
                .AddColumn("AutoUnmonitorAfterDownload").AsBoolean().NotNullable().WithDefaultValue(false);
        }
    }
}
