import { createSelector } from 'reselect';
import createAllSeriesSelector from './createAllSeriesSelector';

function createSeriesCountSelector() {
  return createSelector(
    createAllSeriesSelector(),
    (state) => state.series.error,
    (state) => state.series.isFetching,
    (state) => state.series.isPopulated,
    (series, error, isFetching, isPopulated) => {
      return {
        count: series.length,
        error,
        isFetching,
        isPopulated
      };
    }
  );
}

export default createSeriesCountSelector;
