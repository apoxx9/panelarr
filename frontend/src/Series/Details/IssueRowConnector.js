/* eslint max-params: 0 */
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import IssueRow from './IssueRow';

const selectIssueFiles = createSelector(
  (state) => state.issueFiles,
  (issueFiles) => {
    const { items } = issueFiles;

    return items.reduce((acc, file) => {
      const issueId = file.issueId;
      if (!acc.hasOwnProperty(issueId)) {
        acc[issueId] = [];
      }

      acc[issueId].push(file);

      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createSeriesSelector(),
    selectIssueFiles,
    (state, { id }) => id,
    (series = {}, issueFiles, issueId) => {
      const files = issueFiles[issueId] ?? [];
      const issueFile = files[0];

      return {
        seriesMonitored: series.monitored,
        seriesName: series.seriesName,
        issueFiles: files,
        indexerFlags: issueFile ? issueFile.indexerFlags : 0
      };
    }
  );
}
export default connect(createMapStateToProps)(IssueRow);
