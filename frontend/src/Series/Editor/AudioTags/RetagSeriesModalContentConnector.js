import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createAllSeriesSelector from 'Store/Selectors/createAllSeriesSelector';
import RetagSeriesModalContent from './RetagSeriesModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { seriesIds }) => seriesIds,
    createAllSeriesSelector(),
    (seriesIds, allSeries) => {
      const series = _.intersectionWith(allSeries, seriesIds, (s, id) => {
        return s.id === id;
      });

      const sortedSeries = _.orderBy(series, 'sortName');
      const seriesNames = _.map(sortedSeries, 'seriesName');

      return {
        seriesNames
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand
};

class RetagSeriesModalContentConnector extends Component {

  //
  // Listeners

  onRetagSeriesPress = (updateCovers, embedMetadata) => {
    this.props.executeCommand({
      name: commandNames.RETAG_SERIES,
      seriesIds: this.props.seriesIds,
      updateCovers,
      embedMetadata
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render(props) {
    return (
      <RetagSeriesModalContent
        {...this.props}
        onRetagSeriesPress={this.onRetagSeriesPress}
      />
    );
  }
}

RetagSeriesModalContentConnector.propTypes = {
  seriesIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  onModalClose: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagSeriesModalContentConnector);
