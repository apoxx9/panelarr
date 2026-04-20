import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import IssueIndexPosterOptionsModalContentConnector from './IssueIndexPosterOptionsModalContentConnector';

function IssueIndexPosterOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <IssueIndexPosterOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

IssueIndexPosterOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IssueIndexPosterOptionsModal;
