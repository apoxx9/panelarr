import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createIssuesClientSideCollectionSelector from './createIssuesClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createIssuesClientSideCollectionSelector(uiSection),
    (issues) => {
      const items = issues.items.map((s) => {
        const {
          id,
          title,
          seriesTitle
        } = s;

        return {
          id,
          title,
          seriesTitle
        };
      });

      return {
        ...issues,
        items
      };
    }
  );
}

function issueListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createIssueEqualSelector = createSelectorCreator(
  defaultMemoize,
  issueListEqual
);

function createIssueClientSideCollectionItemsSelector(uiSection) {
  return createIssueEqualSelector(
    createUnoptimizedSelector(uiSection),
    (issue) => issue
  );
}

export default createIssueClientSideCollectionItemsSelector;
