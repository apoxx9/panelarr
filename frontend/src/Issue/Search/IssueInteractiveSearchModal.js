import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import IssueInteractiveSearchModalContent from './IssueInteractiveSearchModalContent';

function IssueInteractiveSearchModal(props) {
  const {
    isOpen,
    issueId,
    issueTitle,
    seriesName,
    onModalClose
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_EXTRA_LARGE}
      closeOnBackgroundClick={false}
      onModalClose={onModalClose}
    >
      <IssueInteractiveSearchModalContent
        issueId={issueId}
        issueTitle={issueTitle}
        seriesName={seriesName}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

IssueInteractiveSearchModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  issueId: PropTypes.number.isRequired,
  issueTitle: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IssueInteractiveSearchModal;
