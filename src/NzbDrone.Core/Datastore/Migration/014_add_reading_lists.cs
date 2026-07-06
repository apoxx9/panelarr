using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Story arcs / reading lists (docs/story-arcs.md): user-curated ordered
    // lists of issues spanning series. ReadingListItems rows are "slots" — they keep
    // foreign ids and display fields even when the linked library issue goes
    // away, so CBL round-trips survive library churn.
    [Migration(14)]
    public class AddReadingLists : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("ReadingLists")
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("ForeignReadingListId").AsString().Nullable().Indexed()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("Publisher").AsString().Nullable()
                .WithColumn("Description").AsString().Nullable()
                .WithColumn("Added").AsDateTime().NotNullable();

            Create.TableForModel("ReadingListItems")
                .WithColumn("ReadingListId").AsInt32().NotNullable().Indexed()
                .WithColumn("Position").AsInt32().NotNullable()
                .WithColumn("IssueId").AsInt32().Nullable().Indexed()
                .WithColumn("ForeignIssueId").AsString().Nullable().Indexed()
                .WithColumn("ForeignSeriesId").AsString().Nullable()
                .WithColumn("SeriesName").AsString().Nullable()
                .WithColumn("IssueNumber").AsString().Nullable()
                .WithColumn("Volume").AsString().Nullable()
                .WithColumn("Year").AsString().Nullable();
        }
    }
}
