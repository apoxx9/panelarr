import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('series', uiSection),
    (series) => {
      const items = series.items.map((s) => {
        const {
          id,
          sortName,
          sortNameLastFirst
        } = s;

        return {
          id,
          sortName,
          sortNameLastFirst
        };
      });

      return {
        ...series,
        items
      };
    }
  );
}

function seriesListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createSeriesEqualSelector = createSelectorCreator(
  defaultMemoize,
  seriesListEqual
);

function createSeriesClientSideCollectionItemsSelector(uiSection) {
  return createSeriesEqualSelector(
    createUnoptimizedSelector(uiSection),
    (series) => series
  );
}

export default createSeriesClientSideCollectionItemsSelector;
