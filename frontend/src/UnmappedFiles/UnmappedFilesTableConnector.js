import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withCurrentPage from 'Components/withCurrentPage';
import { executeCommand } from 'Store/Actions/commandActions';
import { deleteIssueFile, deleteIssueFiles, fetchIssueFiles, setIssueFilesSort, setIssueFilesTableOption } from 'Store/Actions/issueFileActions';
import { fetchRootFolders } from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import UnmappedFilesTable from './UnmappedFilesTable';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('issueFiles'),
    createCommandExecutingSelector(commandNames.RESCAN_FOLDERS),
    createDimensionsSelector(),
    (state) => state.settings.rootFolders,
    (state) => state.series,
    (
      issueFiles,
      isScanningFolders,
      dimensionsState,
      rootFolders,
      series
    ) => {
      // issueFiles could pick up mapped entries via signalR so filter again here
      const {
        items,
        ...otherProps
      } = issueFiles;

      const unmappedFiles = _.filter(items, { issueId: 0 });

      const rootFoldersPopulated = rootFolders.isPopulated;
      const rootFoldersError = rootFolders.error;
      const rootFolderItems = rootFolders.items;
      const seriesItems = series.items;

      // Compute mapped file count from series statistics
      const mappedFileCount = seriesItems.reduce(
        (sum, s) => sum + (s.statistics?.issueFileCount || 0),
        0
      );

      // Determine the empty state type with explicit priority order:
      // 1. Not ready — still loading required data
      // 2. No root folders configured
      // 3. No series added
      // 4. No files found on disk (never scanned or folders empty)
      // 5. All files matched (genuine success)
      // 6. null — has unmapped files, show the table
      let emptyState = null;

      if (!rootFoldersPopulated && !rootFoldersError) {
        // rootFolders still loading — don't evaluate empty states yet,
        // the loading indicator or table will show based on issueFiles state
        emptyState = null;
      } else if (unmappedFiles.length === 0 && otherProps.isPopulated && !otherProps.error) {
        if (rootFolderItems.length === 0) {
          emptyState = 'noRootFolders';
        } else if (seriesItems.length === 0) {
          emptyState = 'noSeries';
        } else if (mappedFileCount === 0) {
          emptyState = 'noFilesFound';
        } else {
          emptyState = 'allMatched';
        }
      }

      return {
        items: unmappedFiles,
        ...otherProps,
        isScanningFolders,
        isSmallScreen: dimensionsState.isSmallScreen,
        emptyState
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setIssueFilesTableOption(payload));
    },

    onSortPress(sortKey) {
      dispatch(setIssueFilesSort({ sortKey }));
    },

    fetchUnmappedFiles() {
      dispatch(fetchIssueFiles({ unmapped: true }));
    },

    dispatchFetchRootFolders() {
      dispatch(fetchRootFolders());
    },

    deleteUnmappedFile(id) {
      dispatch(deleteIssueFile({ id }));
    },

    deleteUnmappedFiles(issueFileIds) {
      dispatch(deleteIssueFiles({ issueFileIds }));
    },

    onAddMissingSeriesPress() {
      dispatch(executeCommand({
        name: commandNames.RESCAN_FOLDERS,
        addNewSeries: true,
        filter: 'matched'
      }));
    }
  };
}

class UnmappedFilesTableConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    registerPagePopulator(this.repopulate, ['issueFileUpdated']);

    this.repopulate();
    this.props.dispatchFetchRootFolders();
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.repopulate);
  }

  //
  // Control

  repopulate = () => {
    this.props.fetchUnmappedFiles();
  };

  //
  // Render

  render() {
    return (
      <UnmappedFilesTable
        {...this.props}
      />
    );
  }
}

UnmappedFilesTableConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  onSortPress: PropTypes.func.isRequired,
  onTableOptionChange: PropTypes.func.isRequired,
  fetchUnmappedFiles: PropTypes.func.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired,
  deleteUnmappedFile: PropTypes.func.isRequired,
  deleteUnmappedFiles: PropTypes.func.isRequired
};

export default withCurrentPage(
  connect(createMapStateToProps, createMapDispatchToProps)(UnmappedFilesTableConnector)
);
