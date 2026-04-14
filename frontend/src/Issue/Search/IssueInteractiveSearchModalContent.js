import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { scrollDirections } from 'Helpers/Props';
import InteractiveSearchConnector from 'InteractiveSearch/InteractiveSearchConnector';
import translate from 'Utilities/String/translate';

function IssueInteractiveSearchModalContent(props) {
  const {
    issueId,
    issueTitle,
    seriesName,
    onModalClose
  } = props;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {issueId === null ?
          translate('InteractiveSearchModalHeader') :
          translate('InteractiveSearchModalHeaderIssueSeries', { issueTitle, seriesName })
        }
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.BOTH}>
        <InteractiveSearchConnector
          type="issue"
          searchPayload={{
            issueId
          }}
        />
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>
          {translate('Close')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

IssueInteractiveSearchModalContent.propTypes = {
  issueId: PropTypes.number.isRequired,
  issueTitle: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default IssueInteractiveSearchModalContent;
