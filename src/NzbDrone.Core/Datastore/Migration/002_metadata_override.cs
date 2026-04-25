using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(2)]
    public class MetadataOverride : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SeriesMetadata")
                .AddColumn("IsOverridden").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("OverriddenFields").AsString().Nullable();

            Alter.Table("Issues")
                .AddColumn("IsOverridden").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("OverriddenFields").AsString().Nullable();
        }
    }
}
