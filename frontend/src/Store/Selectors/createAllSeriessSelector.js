import { createSelector } from 'reselect';

function createAllSeriessSelector() {
  return createSelector(
    (state) => state.seriess,
    (series) => {
      return series.items;
    }
  );
}

export default createAllSeriessSelector;
