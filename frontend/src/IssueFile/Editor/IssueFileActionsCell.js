import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FileDetailsModal from 'IssueFile/FileDetailsModal';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './IssueFileActionsCell.css';

class IssueFileActionsCell extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false,
      isConfirmDeleteModalOpen: false
    };
  }

  //
  // Listeners

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  onDeleteFilePress = () => {
    this.setState({ isConfirmDeleteModalOpen: true });
  };

  onConfirmDelete = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
    this.props.deleteIssueFile({ id: this.props.id });
  };

  onConfirmDeleteModalClose = () => {
    this.setState({ isConfirmDeleteModalOpen: false });
  };

  //
  // Render

  render() {

    const {
      id,
      path
    } = this.props;

    const {
      isDetailsModalOpen,
      isConfirmDeleteModalOpen
    } = this.state;

    return (
      <TableRowCell className={styles.IssueFileActionsCell}>
        {
          path &&
            <IconButton
              name={icons.INFO}
              onPress={this.onDetailsPress}
            />
        }
        {
          path &&
            <IconButton
              name={icons.DELETE}
              onPress={this.onDeleteFilePress}
            />
        }

        <FileDetailsModal
          isOpen={isDetailsModalOpen}
          onModalClose={this.onDetailsModalClose}
          id={id}
        />

        <ConfirmModal
          isOpen={isConfirmDeleteModalOpen}
          kind={kinds.DANGER}
          title={translate('DeleteIssueFile')}
          message={translate('DeleteIssueFileMessageText', [path])}
          confirmLabel={translate('Delete')}
          onConfirm={this.onConfirmDelete}
          onCancel={this.onConfirmDeleteModalClose}
        />
      </TableRowCell>

    );
  }
}

IssueFileActionsCell.propTypes = {
  id: PropTypes.number.isRequired,
  path: PropTypes.string,
  deleteIssueFile: PropTypes.func.isRequired
};

export default IssueFileActionsCell;
