import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  clearInteractiveImportIssues,
  fetchInteractiveImportIssues,
  saveInteractiveImportItem,
  setInteractiveImportIssuesSort,
  updateInteractiveImportItem } from 'Store/Actions/interactiveImportActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import SelectIssueModalContent from './SelectIssueModalContent';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('interactiveImport.issues'),
    (issues) => {
      return issues;
    }
  );
}

const mapDispatchToProps = {
  fetchInteractiveImportIssues,
  setInteractiveImportIssuesSort,
  clearInteractiveImportIssues,
  updateInteractiveImportItem,
  saveInteractiveImportItem
};

class SelectIssueModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      seriesId
    } = this.props;

    this.props.fetchInteractiveImportIssues({ seriesId });
  }

  componentWillUnmount() {
    // This clears the issues for the queue and hides the queue
    // We'll need another place to store issues for manual import
    this.props.clearInteractiveImportIssues();
  }

  //
  // Listeners

  onSortPress = (sortKey, sortDirection) => {
    this.props.setInteractiveImportIssuesSort({ sortKey, sortDirection });
  };

  onIssueSelect = (issueId) => {
    const issue = _.find(this.props.items, { id: issueId });

    const ids = this.props.ids;

    ids.forEach((id) => {
      this.props.updateInteractiveImportItem({
        id,
        issue,
        rejections: []
      });
    });

    this.props.saveInteractiveImportItem({ ids });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <SelectIssueModalContent
        {...this.props}
        onSortPress={this.onSortPress}
        onIssueSelect={this.onIssueSelect}
      />
    );
  }
}

SelectIssueModalContentConnector.propTypes = {
  ids: PropTypes.arrayOf(PropTypes.number).isRequired,
  seriesId: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  fetchInteractiveImportIssues: PropTypes.func.isRequired,
  setInteractiveImportIssuesSort: PropTypes.func.isRequired,
  clearInteractiveImportIssues: PropTypes.func.isRequired,
  saveInteractiveImportItem: PropTypes.func.isRequired,
  updateInteractiveImportItem: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SelectIssueModalContentConnector);
