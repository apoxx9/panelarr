import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveSeries, saveSeriesOverride, setSeriesValue } from 'Store/Actions/seriesActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditSeriesModalContent from './EditSeriesModalContent';

function createIsPathChangingSelector() {
  return createSelector(
    (state) => state.series.pendingChanges,
    createSeriesSelector(),
    (pendingChanges, series) => {
      const path = pendingChanges.path;

      if (path == null) {
        return false;
      }

      return series.path !== path;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    (state) => state.series,
    createSeriesSelector(),
    createIsPathChangingSelector(),
    (seriesState, series, isPathChanging) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = seriesState;

      const seriesSettings = _.pick(series, [
        'monitored',
        'monitorNewItems',
        'qualityProfileId',
        'path',
        'tags'
      ]);

      const settings = selectSettings(seriesSettings, pendingChanges, saveError);

      return {
        seriesName: series.seriesName,
        isSaving,
        saveError,
        isPathChanging,
        originalPath: series.path,
        item: settings.settings,
        overview: series.overview,
        year: series.year,
        isOverridden: series.isOverridden,
        overriddenFields: series.overriddenFields,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetSeriesValue: setSeriesValue,
  dispatchSaveSeries: saveSeries,
  dispatchSaveSeriesOverride: saveSeriesOverride
};

class EditSeriesModalContentConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      metadataOverrides: {}
    };
  }

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetSeriesValue({ name, value });
  };

  onMetadataChange = ({ name, value }) => {
    this.setState((state) => ({
      metadataOverrides: {
        ...state.metadataOverrides,
        [name]: value
      }
    }));
  };

  onSavePress = (moveFiles) => {
    const { metadataOverrides } = this.state;

    // Save metadata overrides if any were changed
    if (Object.keys(metadataOverrides).length > 0) {
      // Map frontend field names to backend field names
      const fieldMap = {
        seriesName: 'Name',
        overview: 'Overview',
        year: 'Year'
      };

      const fields = {};
      for (const [key, value] of Object.entries(metadataOverrides)) {
        const backendKey = fieldMap[key] || key;
        fields[backendKey] = value;
      }

      this.props.dispatchSaveSeriesOverride({
        id: this.props.seriesId,
        fields
      });
    }

    this.props.dispatchSaveSeries({
      id: this.props.seriesId,
      moveFiles
    });
  };

  //
  // Render

  render() {
    return (
      <EditSeriesModalContent
        {...this.props}
        metadataOverrides={this.state.metadataOverrides}
        onInputChange={this.onInputChange}
        onMetadataChange={this.onMetadataChange}
        onSavePress={this.onSavePress}
        onMoveSeriesPress={this.onMoveSeriesPress}
      />
    );
  }
}

EditSeriesModalContentConnector.propTypes = {
  seriesId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  dispatchSetSeriesValue: PropTypes.func.isRequired,
  dispatchSaveSeries: PropTypes.func.isRequired,
  dispatchSaveSeriesOverride: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditSeriesModalContentConnector);
