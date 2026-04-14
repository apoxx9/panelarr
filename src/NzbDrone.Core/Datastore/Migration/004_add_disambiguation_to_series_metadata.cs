using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(4)]
    public class AddDisambiguationToSeriesMetadata : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SeriesMetadata")
                .AddColumn("Disambiguation").AsString().Nullable();
        }
    }
}
