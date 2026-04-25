import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import IssueInteractiveSearchModalConnector from './Search/IssueInteractiveSearchModalConnector';
import styles from './IssueSearchCell.css';

class IssueSearchCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false
    };
  }

  //
  // Listeners

  onManualSearchPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      issueId,
      issueTitle,
      seriesName,
      isSearching,
      onSearchPress,
      ...otherProps
    } = this.props;

    return (
      <TableRowCell className={styles.IssueSearchCell}>
        <SpinnerIconButton
          name={icons.SEARCH}
          isSpinning={isSearching}
          onPress={onSearchPress}
        />

        <IconButton
          name={icons.INTERACTIVE}
          onPress={this.onManualSearchPress}
        />

        <IssueInteractiveSearchModalConnector
          isOpen={this.state.isDetailsModalOpen}
          issueId={issueId}
          issueTitle={issueTitle}
          seriesName={seriesName}
          onModalClose={this.onDetailsModalClose}
          {...otherProps}
        />

      </TableRowCell>
    );
  }
}

IssueSearchCell.propTypes = {
  issueId: PropTypes.number.isRequired,
  seriesId: PropTypes.number.isRequired,
  issueTitle: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  isSearching: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired
};

export default IssueSearchCell;
