import { createSelector } from 'reselect';
import createIssueSelector from './createIssueSelector';

function createIssueSeriesSelector() {
  return createSelector(
    createIssueSelector(),
    (state) => state.seriess.itemMap,
    (state) => state.seriess.items,
    (issue, seriesMap, allSeriess) => {
      return allSeriess[seriesMap[issue.seriesId]];
    }
  );
}

export default createIssueSeriesSelector;
