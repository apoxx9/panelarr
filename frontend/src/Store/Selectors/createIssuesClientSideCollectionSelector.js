import _ from 'lodash';
import { createSelector } from 'reselect';
import filterCollection from 'Utilities/Array/filterCollection';
import sortCollection from 'Utilities/Array/sortCollection';
import createCustomFiltersSelector from './createCustomFiltersSelector';

function createIssuesClientSideCollectionSelector(uiSection) {
  return createSelector(
    (state) => _.get(state, 'issues'),
    (state) => _.get(state, 'series'),
    (state) => _.get(state, uiSection),
    createCustomFiltersSelector('issues', uiSection),
    (issueState, seriesState, uiSectionState = {}, customFilters) => {
      const state = Object.assign({}, issueState, uiSectionState, { customFilters });

      const issues = state.items;
      for (const issue of issues) {
        issue.series = seriesState.items[seriesState.itemMap[issue.seriesId]];
      }

      const filtered = filterCollection(issues, state);
      const sorted = sortCollection(filtered, state);

      return {
        ...issueState,
        ...uiSectionState,
        customFilters,
        items: sorted,
        totalItems: state.items.length
      };
    }
  );
}

export default createIssuesClientSideCollectionSelector;
