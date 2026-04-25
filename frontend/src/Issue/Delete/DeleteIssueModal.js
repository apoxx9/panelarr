import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import DeleteIssueModalContentConnector from './DeleteIssueModalContentConnector';

function DeleteIssueModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.MEDIUM}
      onModalClose={onModalClose}
    >
      <DeleteIssueModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

DeleteIssueModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default DeleteIssueModal;
