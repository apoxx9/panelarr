/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createIssueSeriesSelector from 'Store/Selectors/createIssueSeriesSelector';
import createIssueQualityProfileSelector from 'Store/Selectors/createIssueQualityProfileSelector';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import createExecutingCommandsSelector from 'Store/Selectors/createExecutingCommandsSelector';

function selectShowSearchAction() {
  return createSelector(
    (state) => state.issueIndex,
    (issueIndex) => {
      const view = issueIndex.view;

      switch (view) {
        case 'posters':
          return issueIndex.posterOptions.showSearchAction;
        case 'banners':
          return issueIndex.bannerOptions.showSearchAction;
        case 'overview':
          return issueIndex.overviewOptions.showSearchAction;
        default:
          return issueIndex.tableOptions.showSearchAction;
      }
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    createIssueSeriesSelector(),
    createIssueQualityProfileSelector(),
    selectShowSearchAction(),
    createExecutingCommandsSelector(),
    (
      issue,
      series,
      qualityProfile,
      showSearchAction,
      executingCommands
    ) => {

      // If a issue is deleted this selector may fire before the parent
      // selectors, which will result in an undefined issue, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show an issue that has no information available.

      if (!issue) {
        return {};
      }

      const isRefreshingIssue = executingCommands.some((command) => {
        return (
          (command.name === commandNames.REFRESH_SERIES &&
            command.body.seriesId === issue.seriesId) ||
          (command.name === commandNames.REFRESH_ISSUE &&
            command.body.issueId === issue.id)
        );
      });

      const isSearchingIssue = executingCommands.some((command) => {
        return (
          (command.name === commandNames.SERIES_SEARCH &&
            command.body.seriesId === issue.seriesId) ||
          (command.name === commandNames.ISSUE_SEARCH &&
            command.body.issueIds.includes(issue.id))
        );
      });

      return {
        ...issue,
        series,
        qualityProfile,
        showSearchAction,
        isRefreshingIssue,
        isSearchingIssue
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchExecuteCommand: executeCommand
};

class IssueIndexItemConnector extends Component {

  //
  // Listeners

  onRefreshIssuePress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.REFRESH_ISSUE,
      issueId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.dispatchExecuteCommand({
      name: commandNames.ISSUE_SEARCH,
      issueIds: [this.props.id]
    });
  };

  //
  // Render

  render() {
    const {
      id,
      component: ItemComponent,
      ...otherProps
    } = this.props;

    if (!id) {
      return null;
    }

    return (
      <ItemComponent
        {...otherProps}
        id={id}
        onRefreshIssuePress={this.onRefreshIssuePress}
        onSearchPress={this.onSearchPress}
      />
    );
  }
}

IssueIndexItemConnector.propTypes = {
  id: PropTypes.number,
  component: PropTypes.elementType.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueIndexItemConnector);
