import { createSelector } from 'reselect';

function createIssueFileSelector() {
  return createSelector(
    (state, { issueFileId }) => issueFileId,
    (state) => state.issueFiles,
    (issueFileId, issueFiles) => {
      if (!issueFileId) {
        return;
      }

      return issueFiles.items.find((issueFile) => issueFile.id === issueFileId);
    }
  );
}

export default createIssueFileSelector;
