import migrateAddSeriesDefaults from './migrateAddSeriesDefaults';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';
import migrateSeriesIndexColumns from './migrateSeriesIndexColumns';
import migrateSeriesSortKey from './migrateSeriesSortKey';

export default function migrate(persistedState) {
  migrateAddSeriesDefaults(persistedState);
  migrateSeriesSortKey(persistedState);
  migrateBlacklistToBlocklist(persistedState);
  migrateSeriesIndexColumns(persistedState);
}
