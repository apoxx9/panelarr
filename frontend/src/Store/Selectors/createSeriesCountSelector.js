import { createSelector } from 'reselect';
import createAllSeriessSelector from './createAllSeriessSelector';

function createSeriesCountSelector() {
  return createSelector(
    createAllSeriessSelector(),
    (state) => state.seriess.error,
    (state) => state.seriess.isFetching,
    (state) => state.seriess.isPopulated,
    (seriess, error, isFetching, isPopulated) => {
      return {
        count: seriess.length,
        error,
        isFetching,
        isPopulated
      };
    }
  );
}

export default createSeriesCountSelector;
