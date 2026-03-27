import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';

class EditIssueModalContent extends Component {

  //
  // Listeners

  onSavePress = () => {
    const {
      onSavePress
    } = this.props;

    onSavePress(false);

  };

  //
  // Render

  render() {
    const {
      title,
      seriesName,
      statistics,
      item,
      isFetching,
      isPopulated,
      error,
      isSaving,
      onInputChange,
      onModalClose,
      ...otherProps
    } = this.props;

    const {
      monitored,
      anyEditionOk,
      editions
    } = item;

    const hasFile = statistics ? statistics.issueFileCount > 0 : false;
    const errorMessage = getErrorMessage(error, 'Unable to load editions');

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Edit - {seriesName} - {title}
        </ModalHeader>

        <ModalBody>
          <Form
            {...otherProps}
          >
            <FormGroup>
              <FormLabel>
                {translate('Monitored')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="monitored"
                helpText={translate('MonitoredHelpText')}
                {...monitored}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('AutomaticallySwitchEdition')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.CHECK}
                name="anyEditionOk"
                helpText={translate('AnyEditionOkHelpText')}
                {...anyEditionOk}
                onChange={onInputChange}
              />
            </FormGroup>

            {
              isFetching &&
                <LoadingIndicator />
            }

            {
              error &&
                <div>{errorMessage}</div>
            }

            {
              isPopulated && !isFetching && !!editions.value.length &&
                <FormGroup>
                  <FormLabel>
                    {translate('Edition')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.ISSUE_EDITION_SELECT}
                    name="editions"
                    helpText={translate('EditionsHelpText')}
                    isDisabled={anyEditionOk.value && hasFile}
                    issueEditions={editions}
                    onChange={onInputChange}
                  />
                </FormGroup>
            }

          </Form>
        </ModalBody>
        <ModalFooter>
          <Button
            onPress={onModalClose}
          >
            Cancel
          </Button>

          <SpinnerButton
            isSpinning={isSaving}
            onPress={this.onSavePress}
          >
            Save
          </SpinnerButton>
        </ModalFooter>

      </ModalContent>
    );
  }
}

EditIssueModalContent.propTypes = {
  issueId: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  statistics: PropTypes.object.isRequired,
  item: PropTypes.object.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isPopulated: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditIssueModalContent;
