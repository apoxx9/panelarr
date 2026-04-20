import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import IssueIndexFooter from './IssueIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('issues', 'issueIndex'),
    (issues) => {
      return issues.items.map((s) => {
        const {
          seriesId,
          monitored,
          status,
          statistics
        } = s;

        return {
          seriesId,
          monitored,
          status,
          statistics
        };
      });
    }
  );
}

function createIssueSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (issue) => issue
  );
}

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    (issue) => {
      return {
        issue
      };
    }
  );
}

export default connect(createMapStateToProps)(IssueIndexFooter);
