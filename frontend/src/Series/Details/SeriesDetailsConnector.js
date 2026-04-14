/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { toggleSeriesMonitored } from 'Store/Actions/seriesActions';
import { clearIssueFiles, fetchIssueFiles } from 'Store/Actions/issueFileActions';
import { saveIssueEditor } from 'Store/Actions/issueIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import { clearQueueDetails, fetchQueueDetails } from 'Store/Actions/queueActions';
import { cancelFetchReleases, clearReleases } from 'Store/Actions/releaseActions';
import { clearSeries } from 'Store/Actions/seriesCollectionActions';
import { fetchSeries } from 'Store/Actions/seriesActions';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import SeriesDetails from './SeriesDetails';

const selectIssues = createSelector(
  (state) => state.issues,
  (state) => state.issueIndex,
  (issues, index) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = issues;

    const {
      isSaving,
      saveError,
      isDeleting,
      deleteError
    } = index;

    const hasIssues = !!items.length;
    const hasMonitoredIssues = items.some((e) => e.monitored);

    return {
      isIssuesFetching: isFetching,
      isIssuesPopulated: isPopulated,
      issuesError: error,
      hasIssues,
      hasMonitoredIssues,
      isSaving,
      saveError,
      isDeleting,
      deleteError
    };
  }
);

const selectSeries = createSelector(
  (state) => state.series,
  (series) => {
    const {
      items,
      isFetching,
      isPopulated,
      error
    } = series;

    const hasSeries = !!items.length;

    return {
      isSeriesFetching: isFetching,
      isSeriesPopulated: isPopulated,
      seriesError: error,
      hasSeries,
      series: items
    };
  }
);

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
    selectIssues,
    selectSeries,
    selectIssueFiles,
    createAllSeriesSelector(),
    createCommandsSelector(),
    createDimensionsSelector(),
    (titleSlug, issues, seriesState, issueFiles, allSeries, commands, dimensions) => {
      const sortedSeries = _.orderBy(allSeries, 'sortNameLastFirst');
      const seriesIndex = _.findIndex(sortedSeries, { titleSlug });
      const series = sortedSeries[seriesIndex];

      if (!series) {
        return {};
      }

      const {
        isIssuesFetching,
        isIssuesPopulated,
        issuesError,
        hasIssues,
        hasMonitoredIssues,
        isSaving,
        saveError,
        isDeleting,
        deleteError
      } = issues;

      const {
        isSeriesFetching,
        isSeriesPopulated,
        seriesError,
        hasSeries,
        series: seriesItems
      } = seriesState;

      const {
        isIssueFilesFetching,
        isIssueFilesPopulated,
        issueFilesError,
        hasIssueFiles
      } = issueFiles;

      const previousSeries = sortedSeries[seriesIndex - 1] || _.last(sortedSeries);
      const nextSeries = sortedSeries[seriesIndex + 1] || _.first(sortedSeries);
      const isSeriesRefreshing = isCommandExecuting(findCommand(commands, { name: commandNames.REFRESH_SERIES, seriesId: series.id }));
      const seriesRefreshingCommand = findCommand(commands, { name: commandNames.REFRESH_SERIES });
      const allSeriesRefreshing = (
        isCommandExecuting(seriesRefreshingCommand) &&
        !seriesRefreshingCommand.body.seriesId
      );
      const isRefreshing = isSeriesRefreshing || allSeriesRefreshing;
      const isSearching = isCommandExecuting(findCommand(commands, { name: commandNames.SERIES_SEARCH, seriesId: series.id }));
      const isRenamingFiles = isCommandExecuting(findCommand(commands, { name: commandNames.RENAME_FILES, seriesId: series.id }));
      const isRenamingSeriesCommand = findCommand(commands, { name: commandNames.RENAME_SERIES });
      const isRenamingSeries = (
        isCommandExecuting(isRenamingSeriesCommand) &&
        isRenamingSeriesCommand.body.seriesIds.indexOf(series.id) > -1
      );

      const isFetching = isIssuesFetching || isSeriesFetching || isIssueFilesFetching;
      const isPopulated = isIssuesPopulated && isSeriesPopulated && isIssueFilesPopulated;

      const alternateTitles = _.reduce(series.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...series,
        alternateTitles,
        isSeriesRefreshing,
        allSeriesRefreshing,
        isRefreshing,
        isSearching,
        isRenamingFiles,
        isRenamingSeries,
        isFetching,
        isPopulated,
        issuesError,
        isSaving,
        saveError,
        isDeleting,
        deleteError,
        seriesError,
        issueFilesError,
        hasIssues,
        hasMonitoredIssues,
        hasSeries,
        series: seriesItems,
        hasIssueFiles,
        previousSeries,
        nextSeries,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  fetchSeries,
  clearSeries,
  saveIssueEditor,
  fetchIssueFiles,
  clearIssueFiles,
  toggleSeriesMonitored,
  fetchQueueDetails,
  clearQueueDetails,
  clearReleases,
  cancelFetchReleases,
  executeCommand
};

class SeriesDetailsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.populate);
    this.populate();
  }

  componentDidUpdate(prevProps) {
    const {
      id,
      isSeriesRefreshing,
      allSeriesRefreshing,
      isRenamingFiles,
      isRenamingSeries
    } = this.props;

    if (
      (prevProps.isSeriesRefreshing && !isSeriesRefreshing) ||
      (prevProps.allSeriesRefreshing && !allSeriesRefreshing) ||
      (prevProps.isRenamingFiles && !isRenamingFiles) ||
      (prevProps.isRenamingSeries && !isRenamingSeries)
    ) {
      this.populate();
    }

    // If the id has changed we need to clear the issues
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
    const seriesId = this.props.id;

    this.props.fetchSeries({ seriesId });
    this.props.fetchIssueFiles({ seriesId });
    this.props.fetchQueueDetails({ seriesId });
  };

  unpopulate = () => {
    this.props.cancelFetchReleases();
    this.props.clearSeries();
    this.props.clearIssueFiles();
    this.props.clearQueueDetails();
    this.props.clearReleases();
  };

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleSeriesMonitored({
      seriesId: this.props.id,
      monitored
    });
  };

  onRefreshPress = () => {
    this.props.executeCommand({
      name: commandNames.REFRESH_SERIES,
      seriesId: this.props.id
    });
  };

  onSearchPress = () => {
    this.props.executeCommand({
      name: commandNames.SERIES_SEARCH,
      seriesId: this.props.id
    });
  };

  onSaveSelected = (payload) => {
    this.props.saveIssueEditor(payload);
  };

  //
  // Render

  render() {
    return (
      <SeriesDetails
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
        onRefreshPress={this.onRefreshPress}
        onSearchPress={this.onSearchPress}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

SeriesDetailsConnector.propTypes = {
  id: PropTypes.number.isRequired,
  titleSlug: PropTypes.string.isRequired,
  isSeriesRefreshing: PropTypes.bool.isRequired,
  allSeriesRefreshing: PropTypes.bool.isRequired,
  isRefreshing: PropTypes.bool.isRequired,
  isRenamingFiles: PropTypes.bool.isRequired,
  isRenamingSeries: PropTypes.bool.isRequired,
  fetchSeries: PropTypes.func.isRequired,
  clearSeries: PropTypes.func.isRequired,
  saveIssueEditor: PropTypes.func.isRequired,
  fetchIssueFiles: PropTypes.func.isRequired,
  clearIssueFiles: PropTypes.func.isRequired,
  toggleSeriesMonitored: PropTypes.func.isRequired,
  fetchQueueDetails: PropTypes.func.isRequired,
  clearQueueDetails: PropTypes.func.isRequired,
  clearReleases: PropTypes.func.isRequired,
  cancelFetchReleases: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SeriesDetailsConnector);
