/* eslint max-params: 0 */
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { saveSeriesEditor, setSeriesFilter, setSeriesSort, setSeriesTableOption, setSeriesView } from 'Store/Actions/seriesIndexActions';
import { executeCommand } from 'Store/Actions/commandActions';
import scrollPositions from 'Store/scrollPositions';
import createSeriesClientSideCollectionItemsSelector from 'Store/Selectors/createSeriesClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import SeriesIndex from './SeriesIndex';

function createMapStateToProps() {
  return createSelector(
    createSeriesClientSideCollectionItemsSelector('seriesIndex'),
    createCommandExecutingSelector(commandNames.BULK_REFRESH_SERIES),
    createCommandExecutingSelector(commandNames.RSS_SYNC),
    createCommandExecutingSelector(commandNames.RENAME_SERIES),
    createCommandExecutingSelector(commandNames.RETAG_SERIES),
    createDimensionsSelector(),
    (
      series,
      isRefreshingSeries,
      isRssSyncExecuting,
      isOrganizingSeries,
      isRetaggingSeries,
      dimensionsState
    ) => {
      return {
        ...series,
        isRefreshingSeries,
        isRssSyncExecuting,
        isOrganizingSeries,
        isRetaggingSeries,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setSeriesTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setSeriesSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setSeriesFilter({ selectedFilterKey }));
    },

    dispatchSetSeriesView(view) {
      dispatch(setSeriesView({ view }));
    },

    dispatchSaveSeriesEditor(payload) {
      dispatch(saveSeriesEditor(payload));
    },

    onRefreshSeriesPress(items) {
      dispatch(executeCommand({
        name: commandNames.BULK_REFRESH_SERIES,
        seriesIds: items
      }));
    },

    onRssSyncPress() {
      dispatch(executeCommand({
        name: commandNames.RSS_SYNC
      }));
    }
  };
}

class SeriesIndexConnector extends Component {

  //
  // Listeners

  onViewSelect = (view) => {
    this.props.dispatchSetSeriesView(view);
  };

  onSaveSelected = (payload) => {
    this.props.dispatchSaveSeriesEditor(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.seriesIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <SeriesIndex
        {...this.props}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

SeriesIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  dispatchSetSeriesView: PropTypes.func.isRequired,
  dispatchSaveSeriesEditor: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(SeriesIndexConnector),
  'seriesIndex'
);
