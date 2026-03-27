import migrateAddSeriesDefaults from './migrateAddSeriesDefaults';
import migrateSeriesSortKey from './migrateSeriesSortKey';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';

export default function migrate(persistedState) {
  migrateAddSeriesDefaults(persistedState);
  migrateSeriesSortKey(persistedState);
  migrateBlacklistToBlocklist(persistedState);
}
