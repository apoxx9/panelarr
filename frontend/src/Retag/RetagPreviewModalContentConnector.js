import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { fetchRetagPreview } from 'Store/Actions/retagPreviewActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import RetagPreviewModalContent from './RetagPreviewModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.retagPreview,
    createSeriesSelector(),
    (retagPreview, series) => {
      const props = { ...retagPreview };
      props.isFetching = retagPreview.isFetching;
      props.isPopulated = retagPreview.isPopulated;
      props.error = retagPreview.error;
      props.path = series.path;

      return props;
    }
  );
}

const mapDispatchToProps = {
  fetchRetagPreview,
  executeCommand
};

class RetagPreviewModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      seriesId,
      issueId
    } = this.props;

    this.props.fetchRetagPreview({
      seriesId,
      issueId
    });
  }

  //
  // Listeners

  onRetagPress = (files, updateCovers, embedMetadata) => {
    this.props.executeCommand({
      name: commandNames.RETAG_FILES,
      seriesId: this.props.seriesId,
      updateCovers,
      embedMetadata,
      files
    });

    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <RetagPreviewModalContent
        {...this.props}
        onRetagPress={this.onRetagPress}
      />
    );
  }
}

RetagPreviewModalContentConnector.propTypes = {
  seriesId: PropTypes.number.isRequired,
  issueId: PropTypes.number,
  isPopulated: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  fetchRetagPreview: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(RetagPreviewModalContentConnector);
