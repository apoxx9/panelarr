import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
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
      issueNumber,
      releaseDate,
      pageCount,
      item,
      isSaving,
      isOverridden,
      overriddenFields,
      metadataOverrides,
      onInputChange,
      onMetadataChange,
      onModalClose,
      ...otherProps
    } = this.props;

    const {
      monitored
    } = item;

    const overridden = (overriddenFields || '').split(',').filter(Boolean);

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

            <div style={{ marginTop: 20, paddingTop: 15, borderTop: '1px solid var(--borderColor)' }}>
              <div style={{ fontSize: 16, fontWeight: 'bold', marginBottom: 15 }}>
                Metadata Overrides
                {isOverridden &&
                  <span style={{ fontSize: 12, fontWeight: 'normal', marginLeft: 10, padding: '2px 8px', borderRadius: 3, backgroundColor: 'var(--warningColor)', color: 'var(--white)' }}>
                    {overridden.length} field(s) overridden
                  </span>
                }
              </div>

              <FormGroup>
                <FormLabel>
                  Title
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="title"
                  value={metadataOverrides.title !== undefined ? metadataOverrides.title : (title || '')}
                  helpText={overridden.includes('Title') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Issue Number
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="issueNumber"
                  value={metadataOverrides.issueNumber !== undefined ? metadataOverrides.issueNumber : (issueNumber || '')}
                  helpText={overridden.includes('IssueNumber') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Page Count
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="pageCount"
                  value={metadataOverrides.pageCount !== undefined ? metadataOverrides.pageCount : (pageCount || 0)}
                  helpText={overridden.includes('PageCount') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>
            </div>

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
  issueNumber: PropTypes.string,
  releaseDate: PropTypes.string,
  pageCount: PropTypes.number,
  statistics: PropTypes.object.isRequired,
  item: PropTypes.object.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isOverridden: PropTypes.bool,
  overriddenFields: PropTypes.string,
  metadataOverrides: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onMetadataChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditIssueModalContent;
