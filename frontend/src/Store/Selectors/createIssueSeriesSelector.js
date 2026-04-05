import { createSelector } from 'reselect';
import createIssueSelector from './createIssueSelector';

function createIssueSeriesSelector() {
  return createSelector(
    createIssueSelector(),
    (state) => state.series.itemMap,
    (state) => state.series.items,
    (issue, seriesMap, allSeries) => {
      return allSeries[seriesMap[issue.seriesId]];
    }
  );
}

export default createIssueSeriesSelector;
