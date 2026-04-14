import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearInteractiveImportIssueFiles, fetchInteractiveImportIssueFiles } from 'Store/Actions/interactiveImportActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import ConfirmImportModalContent from './ConfirmImportModalContent';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('interactiveImport.issueFiles'),
    (issueFiles) => {
      return issueFiles;
    }
  );
}

const mapDispatchToProps = {
  fetchInteractiveImportIssueFiles,
  clearInteractiveImportIssueFiles
};

class ConfirmImportModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      issues
    } = this.props;

    this.props.fetchInteractiveImportIssueFiles({ issueId: issues.map((x) => x.id) });
  }

  componentWillUnmount() {
    this.props.clearInteractiveImportIssueFiles();
  }

  //
  // Render

  render() {
    return (
      <ConfirmImportModalContent
        {...this.props}
      />
    );
  }
}

ConfirmImportModalContentConnector.propTypes = {
  issues: PropTypes.arrayOf(PropTypes.object).isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  fetchInteractiveImportIssueFiles: PropTypes.func.isRequired,
  clearInteractiveImportIssueFiles: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ConfirmImportModalContentConnector);
