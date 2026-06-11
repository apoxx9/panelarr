import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import { fetchTranslations } from 'Store/Actions/appActions';
import { fetchStatus } from 'Store/Actions/systemActions';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import SetupWizard from './SetupWizard';

function createMapStateToProps() {
  return createSelector(
    createSystemStatusSelector(),
    (state) => state.system.status.isPopulated,
    (systemStatus, isPopulated) => {
      return {
        systemStatus,
        isPopulated
      };
    }
  );
}

function createMapDispatchToProps(dispatch) {
  return {
    dispatchFetchStatus() {
      dispatch(fetchStatus());
    },
    dispatchFetchTranslations() {
      dispatch(fetchTranslations());
    }
  };
}

class SetupWizardConnector extends Component {

  componentDidMount() {
    this.props.dispatchFetchStatus();
    this.props.dispatchFetchTranslations();
  }

  render() {
    const {
      isPopulated,
      systemStatus,
      dispatchFetchStatus,
      dispatchFetchTranslations,
      ...otherProps
    } = this.props;

    if (!isPopulated) {
      return null;
    }

    return (
      <SetupWizard
        systemStatus={systemStatus}
        {...otherProps}
      />
    );
  }
}

SetupWizardConnector.propTypes = {
  isPopulated: PropTypes.bool.isRequired,
  systemStatus: PropTypes.object.isRequired,
  dispatchFetchStatus: PropTypes.func.isRequired,
  dispatchFetchTranslations: PropTypes.func.isRequired
};

export default withRouter(
  connect(createMapStateToProps, createMapDispatchToProps)(SetupWizardConnector)
);
