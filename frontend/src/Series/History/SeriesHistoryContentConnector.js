import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { seriesHistoryMarkAsFailed, clearSeriesHistory, fetchSeriesHistory } from 'Store/Actions/seriesHistoryActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.seriesHistory,
    (seriesHistory) => {
      return seriesHistory;
    }
  );
}

const mapDispatchToProps = {
  fetchSeriesHistory,
  clearSeriesHistory,
  seriesHistoryMarkAsFailed
};

class SeriesHistoryContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      seriesId,
      issueId
    } = this.props;

    this.props.fetchSeriesHistory({
      seriesId,
      issueId
    });
  }

  componentWillUnmount() {
    this.props.clearSeriesHistory();
  }

  //
  // Listeners

  onMarkAsFailedPress = (historyId) => {
    const {
      seriesId,
      issueId
    } = this.props;

    this.props.seriesHistoryMarkAsFailed({
      historyId,
      seriesId,
      issueId
    });
  };

  //
  // Render

  render() {
    const {
      component: ViewComponent,
      ...otherProps
    } = this.props;

    return (
      <ViewComponent
        {...otherProps}
        onMarkAsFailedPress={this.onMarkAsFailedPress}
      />
    );
  }
}

SeriesHistoryContentConnector.propTypes = {
  component: PropTypes.elementType.isRequired,
  seriesId: PropTypes.number.isRequired,
  issueId: PropTypes.number,
  fetchSeriesHistory: PropTypes.func.isRequired,
  clearSeriesHistory: PropTypes.func.isRequired,
  seriesHistoryMarkAsFailed: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SeriesHistoryContentConnector);
