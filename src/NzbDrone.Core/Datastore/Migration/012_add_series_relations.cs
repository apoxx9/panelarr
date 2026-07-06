using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Related-series links (feature-landscape #11): a lightweight, typed,
    // directional association between two library series (annual, spin-off,
    // related) rendered symmetrically in the UI. Display-only — no effect on
    // matching, refresh, or file management (the settled annuals decision).
    [Migration(12)]
    public class AddSeriesRelations : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("SeriesRelations")
                .WithColumn("SeriesId").AsInt32().NotNullable().Indexed()
                .WithColumn("RelatedSeriesId").AsInt32().NotNullable().Indexed()
                .WithColumn("RelationType").AsInt32().NotNullable();
        }
    }
}
