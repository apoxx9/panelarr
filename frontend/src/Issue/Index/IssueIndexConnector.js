/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { saveIssueEditor, setIssueFilter, setIssueSort, setIssueTableOption, setIssueView } from 'Store/Actions/issueIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createIssueClientSideCollectionItemsSelector from 'Store/Selectors/createIssueClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import IssueIndex from './IssueIndex';

function createMapStateToProps() {
  return createSelector(
    createIssueClientSideCollectionItemsSelector('issueIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_SERIES),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_ISSUE),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.CUTOFF_UNMET_ISSUE_SEARCH),
    createCommandExecutingSelector(commandNames.MISSING_ISSUE_SEARCH),
    createDimensionsSelector(),
    (
      issue,
      isRefreshingSeriesCommand,
      isRefreshingIssueCommand,
      isRssSyncExecuting,
      isCutoffIssuesSearch,
      isMissingIssuesSearch,
      dimensionsState
    ) => {
      const isRefreshingIssue = isRefreshingIssueCommand || isRefreshingSeriesCommand;
      return {
        ...issue,
        isRefreshingIssue,
        isRssSyncExecuting,
        isSearching: isCutoffIssuesSearch || isMissingIssuesSearch,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setIssueTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setIssueSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setIssueFilter({ selectedFilterKey }));
    },

    dispatchSetIssueView(view) {
      dispatch(setIssueView({ view }));
    },

    dispatchSaveIssueEditor(payload) {
      dispatch(saveIssueEditor(payload));
    },

    onRefreshIssuePress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_ISSUE,
        issueIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    },

    onSearchPress(items) {
      dispatch(executeCommand({
        name: commandNames.ISSUE_SEARCH,
        issueIds: items
      }));
    }
  };
}

class IssueIndexConnector extends Component {

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetIssueView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveIssueEditor(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.issueIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <IssueIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

IssueIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchSetIssueView: PropTypes.func.isRequired,
  dispatchSaveIssueEditor: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(IssueIndexConnector),
  'issueIndex'
);

