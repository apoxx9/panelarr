import PropTypes from 'prop-types';
import React from 'react';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditMetadataProfileModalContent.css';

function EditMetadataProfileModalContent(props) {
  const {
    isFetching,
    error,
    isSaving,
    saveError,
    item,
    isInUse,
    onInputChange,
    onSavePress,
    onModalClose,
    onDeleteMetadataProfilePress,
    ...otherProps
  } = props;

  const {
    id,
    name,
    minPopularity,
    skipMissingDate,

    skipPartsAndSets,
    skipSeriesSecondary,
    allowedLanguages,
    ignored,
    minPages
  } = item;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? 'Edit Metadata Profile' : 'Add Metadata Profile'}
      </ModalHeader>

      <ModalBody>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToAddANewMetadataProfilePleaseTryAgain')}
            </div>
        }

        {
          !isFetching && !error &&
            <Form {...otherProps}>
              <Alert kind={kinds.INFO}>
                Controls which issues are automatically monitored when you add a comic series. Issues that don't match these filters will still appear but won't be monitored for download. Use the 'None' profile to only track issues you add manually or that match existing files.
              </Alert>

              <FormGroup>
                <FormLabel>
                  {translate('Name')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="name"
                  {...name}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('MinimumRating')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="minPopularity"
                  {...minPopularity}
                  helpText={translate('MinRatingHelpText')}
                  isFloat={true}
                  min={0}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('MinimumPages')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="minPages"
                  {...minPages}
                  helpText={translate('MinPagesHelpText')}
                  min={0}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('SkipIssuesWithMissingReleaseDate')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="skipMissingDate"
                  {...skipMissingDate}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('SkipCollectedEditions')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="skipPartsAndSets"
                  helpText="Skip collected editions such as trade paperbacks, omnibuses, and hardcover collections"
                  {...skipPartsAndSets}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('SkipSecondarySeriesIssues')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="skipSeriesSecondary"
                  {...skipSeriesSecondary}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('AllowedLanguages')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="allowedLanguages"
                  {...allowedLanguages}
                  helpText={translate('Iso639-3')}
                  onChange={onInputChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  {translate('MustNotContain')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT_TAG}
                  name="ignored"
                  helpText={translate('IgnoredMetaHelpText')}
                  kind={kinds.DANGER}
                  placeholder={translate('IgnoredPlaceHolder')}
                  delimiters={['Tab', 'Enter', ',']}
                  {...ignored}
                  onChange={onInputChange}
                />
              </FormGroup>

            </Form>
        }
      </ModalBody>
      <ModalFooter>
        {
          id &&
            <div
              className={styles.deleteButtonContainer}
              title={isInUse ? translate('IsInUseCantDeleteAMetadataProfileThatIsAttachedToAnSeriesOrImportList') : undefined}
            >
              <Button
                kind={kinds.DANGER}
                isDisabled={isInUse}
                onPress={onDeleteMetadataProfilePress}
              >
                Delete
              </Button>
            </div>
        }

        <Button
          onPress={onModalClose}
        >
          Cancel
        </Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          Save
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

EditMetadataProfileModalContent.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  isInUse: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteMetadataProfilePress: PropTypes.func
};

export default EditMetadataProfileModalContent;
