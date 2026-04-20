import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllSeriesSelector from './createAllSeriesSelector';

function createExistingSeriesSelector() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    createAllSeriesSelector(),
    (titleSlug, series) => {
      return _.some(series, { titleSlug });
    }
  );
}

export default createExistingSeriesSelector;
