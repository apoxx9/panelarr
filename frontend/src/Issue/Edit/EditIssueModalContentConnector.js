import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveIssue, setIssueValue } from 'Store/Actions/issueActions';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import selectSettings from 'Store/Selectors/selectSettings';
import EditIssueModalContent from './EditIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.issues,
    createIssueSelector(),
    createSeriesSelector(),
    (issueState, issue, series) => {
      const {
        isSaving,
        saveError,
        pendingChanges
      } = issueState;

      const issueSettings = _.pick(issue, [
        'monitored'
      ]);

      const settings = selectSettings(issueSettings, pendingChanges, saveError);

      return {
        title: issue.title,
        seriesName: series.seriesName,
        issueType: issue.issueType,
        statistics: issue.statistics,
        isSaving,
        saveError,
        item: settings.settings,
        ...settings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetIssueValue: setIssueValue,
  dispatchSaveIssue: saveIssue
};

class EditIssueModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.dispatchSetIssueValue({ name, value });
  };

  onSavePress = () => {
    this.props.dispatchSaveIssue({
      id: this.props.issueId
    });
  };

  //
  // Render

  render() {
    return (
      <EditIssueModalContent
        {...this.props}
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
      />
    );
  }
}

EditIssueModalContentConnector.propTypes = {
  issueId: PropTypes.number,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  dispatchSetIssueValue: PropTypes.func.isRequired,
  dispatchSaveIssue: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditIssueModalContentConnector);
