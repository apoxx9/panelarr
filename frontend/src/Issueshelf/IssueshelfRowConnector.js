import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleSeriesMonitored } from 'Store/Actions/seriesActions';
import { toggleIssuesMonitored } from 'Store/Actions/issueActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import IssueshelfRow from './IssueshelfRow';

// Use a const to share the reselect cache between instances
const getIssueMap = createSelector(
  (state) => state.issues.items,
  (issues) => {
    return issues.reduce((acc, curr) => {
      (acc[curr.seriesId] = acc[curr.seriesId] || []).push(curr);
      return acc;
    }, {});
  }
);

function createMapStateToProps() {
  return createSelector(
    createSeriesSelector(),
    getIssueMap,
    (series, issueMap) => {
      const issuesInSeries = issueMap.hasOwnProperty(series.id) ? issueMap[series.id] : [];
      const sortedIssues = _.orderBy(issuesInSeries, 'releaseDate', 'desc');

      return {
        ...series,
        seriesId: series.id,
        seriesName: series.seriesName,
        monitored: series.monitored,
        status: series.status,
        isSaving: series.isSaving,
        issues: sortedIssues
      };
    }
  );
}

const mapDispatchToProps = {
  toggleSeriesMonitored,
  toggleIssuesMonitored
};

class IssueshelfRowConnector extends Component {

  //
  // Listeners

  onSeriesMonitoredPress = () => {
    const {
      seriesId,
      monitored
    } = this.props;

    this.props.toggleSeriesMonitored({
      seriesId,
      monitored: !monitored
    });
  };

  onIssueMonitoredPress = (issueId, monitored) => {
    const issueIds = [issueId];
    this.props.toggleIssuesMonitored({
      issueIds,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <IssueshelfRow
        {...this.props}
        onSeriesMonitoredPress={this.onSeriesMonitoredPress}
        onIssueMonitoredPress={this.onIssueMonitoredPress}
      />
    );
  }
}

IssueshelfRowConnector.propTypes = {
  seriesId: PropTypes.number.isRequired,
  monitored: PropTypes.bool.isRequired,
  toggleSeriesMonitored: PropTypes.func.isRequired,
  toggleIssuesMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueshelfRowConnector);
