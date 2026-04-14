/* eslint max-params: 0 */
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleSeriesMonitored } from 'Store/Actions/seriesActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import SeriesDetailsHeader from './SeriesDetailsHeader';

function createMapStateToProps() {
  return createSelector(
    (state) => state.series,
    createSeriesSelector(),
    createDimensionsSelector(),
    (seriesState, series, dimensions) => {
      const alternateTitles = _.reduce(series.alternateTitles, (acc, alternateTitle) => {
        if ((alternateTitle.seasonNumber === -1 || alternateTitle.seasonNumber === undefined) &&
            (alternateTitle.sceneSeasonNumber === -1 || alternateTitle.sceneSeasonNumber === undefined)) {
          acc.push(alternateTitle.title);
        }

        return acc;
      }, []);

      return {
        ...series,
        isSaving: seriesState.isSaving,
        alternateTitles,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  toggleSeriesMonitored
};

class SeriesDetailsHeaderConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleSeriesMonitored({
      seriesId: this.props.seriesId,
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <SeriesDetailsHeader
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

SeriesDetailsHeaderConnector.propTypes = {
  seriesId: PropTypes.number.isRequired,
  toggleSeriesMonitored: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SeriesDetailsHeaderConnector);
