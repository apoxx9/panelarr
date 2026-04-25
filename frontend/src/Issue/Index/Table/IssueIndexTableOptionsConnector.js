import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import IssueIndexTableOptions from './IssueIndexTableOptions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.seriesIndex.tableOptions,
    (tableOptions) => {
      return tableOptions;
    }
  );
}

export default connect(createMapStateToProps)(IssueIndexTableOptions);
