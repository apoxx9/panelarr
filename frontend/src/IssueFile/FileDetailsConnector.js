import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { fetchIssueFiles } from 'Store/Actions/issueFileActions';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import FileDetails from './FileDetails';

function createMapStateToProps() {
  return createSelector(
    (state) => state.issueFiles,
    (issueFiles) => {
      return {
        ...issueFiles
      };
    }
  );
}

const mapDispatchToProps = {
  fetchIssueFiles
};

class FileDetailsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchIssueFiles({ id: this.props.id });
  }

  //
  // Render

  render() {
    const {
      items,
      id,
      isFetching,
      error
    } = this.props;

    const item = _.find(items, { id });
    const errorMessage = getErrorMessage(error, 'Unable to load manual import items');

    if (isFetching || !item.fileTags) {
      return (
        <LoadingIndicator />
      );
    } else if (error) {
      return (
        <div>{errorMessage}</div>
      );
    }

    return (
      <FileDetails
        fileTags={item.fileTags}
        filename={item.path}
      />
    );

  }
}

FileDetailsConnector.propTypes = {
  fetchIssueFiles: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  id: PropTypes.number.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(FileDetailsConnector);
