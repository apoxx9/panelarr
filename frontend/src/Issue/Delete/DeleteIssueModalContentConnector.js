import { push } from 'connected-react-router';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteIssue } from 'Store/Actions/issueActions';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import DeleteIssueModalContent from './DeleteIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    (issue) => {
      return issue;
    }
  );
}

const mapDispatchToProps = {
  push,
  deleteIssue
};

class DeleteIssueModalContentConnector extends Component {

  //
  // Listeners

  onDeletePress = (deleteFiles, addImportListExclusion) => {
    this.props.deleteIssue({
      id: this.props.issueId,
      deleteFiles,
      addImportListExclusion
    });

    this.props.onModalClose(true);

    this.props.push(`${window.Panelarr.urlBase}/series/${this.props.seriesSlug}`);
  };

  //
  // Render

  render() {
    return (
      <DeleteIssueModalContent
        {...this.props}
        onDeletePress={this.onDeletePress}
      />
    );
  }
}

DeleteIssueModalContentConnector.propTypes = {
  issueId: PropTypes.number.isRequired,
  seriesSlug: PropTypes.string.isRequired,
  push: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  deleteIssue: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DeleteIssueModalContentConnector);
