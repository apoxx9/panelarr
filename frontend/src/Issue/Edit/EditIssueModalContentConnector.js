import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveIssue, saveIssueOverride, setIssueValue } from 'Store/Actions/issueActions';
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
        issueNumber: issue.issueNumber,
        releaseDate: issue.releaseDate,
        pageCount: issue.pageCount,
        issueType: issue.issueType,
        statistics: issue.statistics,
        isOverridden: issue.isOverridden,
        overriddenFields: issue.overriddenFields,
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
  dispatchSaveIssue: saveIssue,
  dispatchSaveIssueOverride: saveIssueOverride
};

class EditIssueModalContentConnector extends Component {

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
    this.props.dispatchSetIssueValue({ name, value });
  };

  onMetadataChange = ({ name, value }) => {
    this.setState((state) => ({
      metadataOverrides: {
        ...state.metadataOverrides,
        [name]: value
      }
    }));
  };

  onSavePress = () => {
    const { metadataOverrides } = this.state;

    if (Object.keys(metadataOverrides).length > 0) {
      const fieldMap = {
        title: 'Title',
        issueNumber: 'IssueNumber',
        releaseDate: 'ReleaseDate',
        pageCount: 'PageCount'
      };

      const fields = {};
      for (const [key, value] of Object.entries(metadataOverrides)) {
        const backendKey = fieldMap[key] || key;
        fields[backendKey] = value;
      }

      this.props.dispatchSaveIssueOverride({
        id: this.props.issueId,
        fields
      });
    }

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
        metadataOverrides={this.state.metadataOverrides}
        onInputChange={this.onInputChange}
        onMetadataChange={this.onMetadataChange}
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
  dispatchSaveIssueOverride: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditIssueModalContentConnector);
