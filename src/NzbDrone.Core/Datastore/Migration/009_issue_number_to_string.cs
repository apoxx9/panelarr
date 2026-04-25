using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(9)]
    public class IssueNumberToString : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Add new string IssueNumber column and SortOrder
            Alter.Table("Issues").AddColumn("IssueNumberText").AsString().Nullable();
            Alter.Table("Issues").AddColumn("SortOrder").AsFloat().WithDefaultValue(0);

            // Add variant/printing fields
            Alter.Table("Issues").AddColumn("VariantDescription").AsString().Nullable();
            Alter.Table("Issues").AddColumn("PrintingNumber").AsInt32().WithDefaultValue(0);

            // Migrate existing float IssueNumber to string and SortOrder
            // Use CASE to avoid "1.0" for whole numbers — produce "1" instead
            Execute.Sql("UPDATE \"Issues\" SET \"IssueNumberText\" = CASE WHEN \"IssueNumber\" = CAST(CAST(\"IssueNumber\" AS INTEGER) AS REAL) THEN CAST(CAST(\"IssueNumber\" AS INTEGER) AS TEXT) ELSE CAST(\"IssueNumber\" AS TEXT) END, \"SortOrder\" = \"IssueNumber\"");

            // Remove old column and rename new one
            Delete.Column("IssueNumber").FromTable("Issues");
            Rename.Column("IssueNumberText").OnTable("Issues").To("IssueNumber");
        }
    }
}
