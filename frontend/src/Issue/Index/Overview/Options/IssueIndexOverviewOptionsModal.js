import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import IssueIndexOverviewOptionsModalContentConnector from './IssueIndexOverviewOptionsModalContentConnector';

function IssueIndexOverviewOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <IssueIndexOverviewOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

IssueIndexOverviewOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IssueIndexOverviewOptionsModal;
