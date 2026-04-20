import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addSeries, setSeriesAddDefault } from 'Store/Actions/searchActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewSeriesModalContent from './AddNewSeriesModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.search,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (searchState, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        seriesDefaults
      } = searchState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(seriesDefaults, {}, addError);

      return {
        isAdding,
        addError,
        isSmallScreen: dimensions.isSmallScreen,
        validationErrors,
        validationWarnings,
        isWindows: systemStatus.isWindows,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  setSeriesAddDefault,
  addSeries
};

class AddNewSeriesModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setSeriesAddDefault({ [name]: value });
  };

  onAddSeriesPress = (searchForMissingIssues) => {
    const {
      foreignSeriesId,
      rootFolderPath,
      monitor,
      qualityProfileId,
      tags
    } = this.props;

    this.props.addSeries({
      foreignSeriesId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      monitorNewItems: 'all',
      qualityProfileId: qualityProfileId.value,
      tags: tags.value,
      searchForMissingIssues
    });
  };

  //
  // Render

  render() {
    return (
      <AddNewSeriesModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddSeriesPress={this.onAddSeriesPress}
      />
    );
  }
}

AddNewSeriesModalContentConnector.propTypes = {
  foreignSeriesId: PropTypes.string.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setSeriesAddDefault: PropTypes.func.isRequired,
  addSeries: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewSeriesModalContentConnector);
