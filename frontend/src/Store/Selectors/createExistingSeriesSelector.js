import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllSeriessSelector from './createAllSeriessSelector';

function createExistingSeriesSelector() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    createAllSeriessSelector(),
    (titleSlug, series) => {
      return _.some(series, { titleSlug });
    }
  );
}

export default createExistingSeriesSelector;
