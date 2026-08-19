using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Delay profiles only knew usenet and torrent (a Readarr fossil). Direct
    // download (GetComics) is a first-class protocol for comics, so profiles
    // gain an enable flag and a delay for it. Profiles still on the usenet
    // default move to torrent: no comic setup has a usenet indexer, and
    // torrent-first with a DDL fallback is the reliable ordering - torrents
    // finish in minutes where throttled DDL hosts take half an hour, and DDL
    // stays the only source for content trackers do not carry.
    [Migration(15)]
    public class DelayProfileDirectDownload : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DelayProfiles").AddColumn("EnableDirectDownload").AsBoolean().WithDefaultValue(true);
            Alter.Table("DelayProfiles").AddColumn("DirectDownloadDelay").AsInt32().WithDefaultValue(0);

            Execute.Sql("UPDATE \"DelayProfiles\" SET \"PreferredProtocol\" = 2 WHERE \"PreferredProtocol\" = 1");
        }
    }
}
