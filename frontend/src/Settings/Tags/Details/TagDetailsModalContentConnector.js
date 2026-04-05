import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import TagDetailsModalContent from './TagDetailsModalContent';

function findMatchingItems(ids, items) {
  return items.filter((s) => {
    return ids.includes(s.id);
  });
}

function createUnorderedMatchingSeriesSelector() {
  return createSelector(
    (state, { seriesIds }) => seriesIds,
    createAllSeriesSelector(),
    findMatchingItems
  );
}

function createMatchingSeriesSelector() {
  return createSelector(
    createUnorderedMatchingSeriesSelector(),
    (series) => {
      return series.sort((seriesA, seriesB) => {
        const sortNameA = seriesA.sortName;
        const sortNameB = seriesB.sortName;

        if (sortNameA > sortNameB) {
          return 1;
        } else if (sortNameA < sortNameB) {
          return -1;
        }

        return 0;
      });
    }
  );
}

function createMatchingDelayProfilesSelector() {
  return createSelector(
    (state, { delayProfileIds }) => delayProfileIds,
    (state) => state.settings.delayProfiles.items,
    findMatchingItems
  );
}

function createMatchingImportListsSelector() {
  return createSelector(
    (state, { importListIds }) => importListIds,
    (state) => state.settings.importLists.items,
    findMatchingItems
  );
}

function createMatchingNotificationsSelector() {
  return createSelector(
    (state, { notificationIds }) => notificationIds,
    (state) => state.settings.notifications.items,
    findMatchingItems
  );
}

function createMatchingReleaseProfilesSelector() {
  return createSelector(
    (state, { restrictionIds }) => restrictionIds,
    (state) => state.settings.releaseProfiles.items,
    findMatchingItems
  );
}

function createMatchingIndexersSelector() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    (state) => state.settings.indexers.items,
    findMatchingItems
  );
}

function createMatchingDownloadClientsSelector() {
  return createSelector(
    (state, { downloadClientIds }) => downloadClientIds,
    (state) => state.settings.downloadClients.items,
    findMatchingItems
  );
}

function createMapStateToProps() {
  return createSelector(
    createMatchingSeriesSelector(),
    createMatchingDelayProfilesSelector(),
    createMatchingImportListsSelector(),
    createMatchingNotificationsSelector(),
    createMatchingReleaseProfilesSelector(),
    createMatchingIndexersSelector(),
    createMatchingDownloadClientsSelector(),
    (series, delayProfiles, importLists, notifications, releaseProfiles, indexers, downloadClients) => {
      return {
        series,
        delayProfiles,
        importLists,
        notifications,
        releaseProfiles,
        indexers,
        downloadClients
      };
    }
  );
}

export default connect(createMapStateToProps)(TagDetailsModalContent);
