import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditIssueModal from './EditIssueModal';

const mapDispatchToProps = {
  clearPendingChanges
};

class EditIssueModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.clearPendingChanges({ section: 'issues' });
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <EditIssueModal
        {...this.props}
        onModalClose={this.onModalClose}
      />
    );
  }
}

EditIssueModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(undefined, mapDispatchToProps)(EditIssueModalConnector);
