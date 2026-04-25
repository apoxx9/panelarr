import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteIssue } from 'Store/Actions/issueIndexActions';
import DeleteIssueModalContent from './DeleteIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { issueIds }) => issueIds,
    (state) => state.issues.items,
    (state) => state.issueFiles.items,
    (issueIds, allIssues, allIssueFiles) => {
      const selectedIssue = _.intersectionWith(allIssues, issueIds, (s, id) => {
        return s.id === id;
      });

      const sortedIssue = _.orderBy(selectedIssue, 'title');

      const selectedFiles = _.intersectionWith(allIssueFiles, issueIds, (s, id) => {
        return s.issueId === id;
      });

      const files = _.orderBy(selectedFiles, ['issueId', 'path']);

      const issue = _.map(sortedIssue, (s) => {
        return {
          title: s.title,
          path: s.path
        };
      });

      return {
        issue,
        files
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress(deleteFiles, addImportListExclusion) {
      dispatch(bulkDeleteIssue({
        issueIds: props.issueIds,
        deleteFiles,
        addImportListExclusion
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteIssueModalContent);
