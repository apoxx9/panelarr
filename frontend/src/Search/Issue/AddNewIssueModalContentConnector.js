import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { addIssue, setIssueAddDefault } from 'Store/Actions/searchActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import AddNewIssueModalContent from './AddNewIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { isExistingSeries }) => isExistingSeries,
    (state) => state.search,
    createDimensionsSelector(),
    createSystemStatusSelector(),
    (isExistingSeries, searchState, dimensions, systemStatus) => {
      const {
        isAdding,
        addError,
        issueDefaults
      } = searchState;

      const {
        settings,
        validationErrors,
        validationWarnings
      } = selectSettings(issueDefaults, {}, addError);

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
  setIssueAddDefault,
  addIssue
};

class AddNewIssueModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setIssueAddDefault({ [name]: value });
  };

  onAddIssuePress = (searchForNewIssue) => {
    const {
      foreignIssueId,
      rootFolderPath,
      monitor,
      qualityProfileId,
      tags
    } = this.props;

    this.props.addIssue({
      foreignIssueId,
      rootFolderPath: rootFolderPath.value,
      monitor: monitor.value,
      monitorNewItems: 'all',
      qualityProfileId: qualityProfileId.value,
      tags: tags.value,
      searchForNewIssue
    });
  };

  //
  // Render

  render() {
    return (
      <AddNewIssueModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onAddIssuePress={this.onAddIssuePress}
      />
    );
  }
}

AddNewIssueModalContentConnector.propTypes = {
  isExistingSeries: PropTypes.bool.isRequired,
  foreignIssueId: PropTypes.string.isRequired,
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  tags: PropTypes.object.isRequired,
  onModalClose: PropTypes.func.isRequired,
  setIssueAddDefault: PropTypes.func.isRequired,
  addIssue: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddNewIssueModalContentConnector);
