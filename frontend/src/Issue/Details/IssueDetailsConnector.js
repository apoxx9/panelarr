/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { toggleIssuesMonitored } from 'Store/Actions/issueActions';
import { clearIssueFiles, fetchIssueFiles } from 'Store/Actions/issueFileActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import IssueDetails from './IssueDetails';

const selectIssueFiles = createSelector(
  (state) => state.issueFiles,
  (issueFiles) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = issueFiles;

    const hasIssueFiles = !!items.length;

    return {
      isIssueFilesFetching: isFetching,
      isIssueFilesPopulated: isPopulated,
      issueFilesError: error,
      hasIssueFiles
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state, { titleSlug }) => titleSlug,
    selectIssueFiles,
    (state) => state.issues,
    createAllSeriesSelector(),
    createCommandsSelector(),
    createUISettingsSelector(),
    createDimensionsSelector(),
    (titleSlug, issueFiles, issues, allSeries, commands, uiSettings, dimensions) => {
      const issue = issues.items.find((b) => b.titleSlug === titleSlug);
      const series = allSeries.find((a) => a.id === issue.seriesId);
      const sortedIssues = issues.items.filter((b) => b.seriesId === issue.seriesId);
      sortedIssues.sort((a, b) => ((a.releaseDate > b.releaseDate) ? 1 : -1));
      const issueIndex = sortedIssues.findIndex((b) => b.id === issue.id);

      if (!issue) {
        return {};
      }

      const {
        isIssueFilesFetching,
        isIssueFilesPopulated,
        issueFilesError,
        hasIssueFiles
      } = issueFiles;

      const previousIssue = sortedIssues[issueIndex - 1] || _.last(sortedIssues);
      const nextIssue = sortedIssues[issueIndex + 1] || _.first(sortedIssues);
      const isRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_ISSUE });
      const isRefreshing = (
        isCommandExecuting(isRefreshingCommand) &&
        isRefreshingCommand.body.issueId === issue.id
      );
      const isSearchingCommand = findCommand(commands, { name: commandNames.ISSUE_SEARCH });
      const isSearching = (
        isCommandExecuting(isSearchingCommand) &&
        isSearchingCommand.body.issueIds.indexOf(issue.id) > -1
      );
      const isRetaggingCommand = findCommand(commands, { name: commandNames.RETAG_FILES, seriesId: series.id });
      const isRetagging = isCommandExecuting(isRetaggingCommand);
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, seriesId: series.id }));
      const isRenamingSeriesCommand = findCommand(commands, { name: commandNames.RENAME_SERIES });
      const isRenamingSeries = (
        isCommandExecuting(isRenamingSeriesCommand) &&
        isRenamingSeriesCommand.body.seriesIds.indexOf(series.id) > -1
      );

      const isFetching = isIssueFilesFetching;
      const isPopulated = isIssueFilesPopulated;

      return {
        ...issue,
        shortDateFormat: uiSettings.shortDateFormat,
        series,
        isRefreshing,
        isRetagging,
        isSearching,
        isRenamingFiles,
        isRenamingSeries,
        isFetching,
        isPopulated,
        issueFilesError,
        hasIssueFiles,
        previousIssue,
        nextIssue,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand,
  fetchIssueFiles,
  clearIssueFiles,
  clearReleases,
  cancelFetchReleases,
  toggleIssuesMonitored
};


class IssueDetailsConnector extends Component {

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      anyReleaseOk,
      isRenamingFiles,
      isRenamingSeries
    } = this.props;

    if (
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingSeries && !isRenamingSeries) ||
      (prevProps.anyReleaseOk === false && anyReleaseOk === true)
    ) {
      this.unpopulate();
      this.populate();
    }

    // If the id has changed we need to clear the issue
    // files and fetch from the server.

    if (prevProps.id !== id) {
      this.unpopulate();
      this.populate();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.populate);
    this.unpopulate();
  }

  //
  // Control

  populate = () => {
    const issueId = this.props.id;

    this.props.fetchIssueFiles({ issueId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearReleases();
    this.props.clearIssueFiles();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleIssuesMonitored({
      issueIds: [this.props.id],
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_ISSUE,
      issueId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.ISSUE_SEARCH,
      issueIds: [this.props.id]
    });
  };

  //
  // Render

  render() {
    return (
      <IssueDetails
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onSearchPress={this.onSearchPress}
      />
    );
  }
}

IssueDetailsConnector.propTypes = {
  id: PropTypes.number,
  anyReleaseOk: PropTypes.bool,
  isRetagging: PropTypes.bool.isRequired,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingSeries: PropTypes.bool.isRequired,
  isIssueFetching: PropTypes.bool,
  isIssuePopulated: PropTypes.bool,
  titleSlug: PropTypes.string.isRequired,
  fetchIssueFiles: PropTypes.func.isRequired,
  clearIssueFiles: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueDetailsConnector);
