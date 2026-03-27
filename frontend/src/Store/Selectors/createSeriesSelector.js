import { createSelector } from 'reselect';

function createSeriesSelector() {
  return createSelector(
    (state, { seriesId }) => seriesId,
    (state) => state.seriess.itemMap,
    (state) => state.seriess.items,
    (seriesId, itemMap, allSeriess) => {
      return allSeriess[itemMap[seriesId]];
    }
  );
}

export default createSeriesSelector;
