import { createSelector } from 'reselect';

function createQueueItemSelector() {
  return createSelector(
    (state, { issueId }) => issueId,
    (state) => state.queue.details.items,
    (issueId, details) => {
      if (!issueId || !details) {
        return null;
      }

      return details.find((item) => {
        if (item.issue) {
          return item.issue.id === issueId;
        }

        return false;
      });
    }
  );
}

export default createQueueItemSelector;
