import { createSelector } from 'reselect';

function createIssueSelector() {
  return createSelector(
    (state, { issueId }) => issueId,
    (state) => state.issues.itemMap,
    (state) => state.issues.items,
    (issueId, itemMap, allIssues) => {
      return itemMap ? allIssues[itemMap[issueId]] : undefined;
    }
  );
}

export default createIssueSelector;
