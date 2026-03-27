import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import RetagSeriesModalContentConnector from './RetagSeriesModalContentConnector';

function RetagSeriesModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <RetagSeriesModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

RetagSeriesModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default RetagSeriesModal;
