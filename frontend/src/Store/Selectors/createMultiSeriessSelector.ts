import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Series from 'Series/Series';

function createMultiSeriessSelector(seriesIds: number[]) {
  return createSelector(
    (state: AppState) => state.seriess.itemMap,
    (state: AppState) => state.seriess.items,
    (itemMap, allSeriess) => {
      return seriesIds.reduce((acc: Series[], seriesId) => {
        const series = allSeriess[itemMap[seriesId]];

        if (series) {
          acc.push(series);
        }

        return acc;
      }, []);
    }
  );
}

export default createMultiSeriessSelector;
