import migrateAddSeriesDefaults from './migrateAddSeriesDefaults';
import migrateSeriesSortKey from './migrateSeriesSortKey';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';
import migrateSeriesIndexColumns from './migrateSeriesIndexColumns';

export default function migrate(persistedState) {
  migrateAddSeriesDefaults(persistedState);
  migrateSeriesSortKey(persistedState);
  migrateBlacklistToBlocklist(persistedState);
  migrateSeriesIndexColumns(persistedState);
}
