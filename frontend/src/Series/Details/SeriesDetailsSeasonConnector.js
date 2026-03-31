/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSeriesDetailsId, setSeriesDetailsSort } from 'Store/Actions/seriesDetailsActions';
import { setIssuesTableOption, toggleIssuesMonitored } from 'Store/Actions/issueActions';
import { executeCommand } from 'Store/Actions/commandActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import SeriesDetailsSeason from './SeriesDetailsSeason';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('issues', 'seriesDetails'),
    createSeriesSelector(),
    createDimensionsSelector(),
    createUISettingsSelector(),
    (issues, series, dimensions, uiSettings) => {

      const issuesInGroup = issues.items;

      let sortDir = 'asc';

      if (issues.sortDirection === 'descending') {
        sortDir = 'desc';
      }

      const effectiveSortKey = issues.sortKey === 'issue' ? 'issueNumber' : issues.sortKey;
      const sortedIssues = _.orderBy(issuesInGroup, effectiveSortKey, sortDir);

      return {
        items: sortedIssues,
        columns: issues.columns,
        sortKey: issues.sortKey,
        sortDirection: issues.sortDirection,
        seriesMonitored: series.monitored,
        isSmallScreen: dimensions.isSmallScreen,
        uiSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setSeriesDetailsId,
  setSeriesDetailsSort,
  toggleIssuesMonitored,
  setIssuesTableOption,
  executeCommand
};

class SeriesDetailsSeasonConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.setSeriesDetailsId({ seriesId: this.props.seriesId });
  }

  //
  // Listeners

  onTableOptionChange = (payload) => {
    this.props.setIssuesTableOption(payload);
  };

  onSortPress = (sortKey) => {
    this.props.setSeriesDetailsSort({ sortKey });
  };

  onMonitorIssuePress = (issueIds, monitored) => {
    this.props.toggleIssuesMonitored({
      issueIds,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <SeriesDetailsSeason
        {...this.props}
        onSortPress={this.onSortPress}
        onTableOptionChange={this.onTableOptionChange}
        onMonitorIssuePress={this.onMonitorIssuePress}
      />
    );
  }
}

SeriesDetailsSeasonConnector.propTypes = {
  seriesId: PropTypes.number.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  setIssuesTableOption: PropTypes.func.isRequired,
  setSeriesDetailsId: PropTypes.func.isRequired,
  setSeriesDetailsSort: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SeriesDetailsSeasonConnector);
