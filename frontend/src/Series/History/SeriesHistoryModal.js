import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import SeriesHistoryContentConnector from './SeriesHistoryContentConnector';
import SeriesHistoryModalContent from './SeriesHistoryModalContent';

function SeriesHistoryModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={onModalClose}
    >
      <SeriesHistoryContentConnector
        component={SeriesHistoryModalContent}
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

SeriesHistoryModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default SeriesHistoryModal;
