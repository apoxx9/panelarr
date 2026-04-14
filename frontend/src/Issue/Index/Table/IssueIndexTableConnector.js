import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setIssueSort } from 'Store/Actions/issueIndexActions';
import IssueIndexTable from './IssueIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.issueIndex.tableOptions,
    (state) => state.issueIndex.columns,
    (dimensions, tableOptions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        showBanners: tableOptions.showBanners,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setIssueSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexTable);
