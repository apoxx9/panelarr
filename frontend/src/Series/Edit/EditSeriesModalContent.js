import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesMonitorNewItemsOptionsPopoverContent from 'AddSeries/SeriesMonitorNewItemsOptionsPopoverContent';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, kinds, tooltipPositions } from 'Helpers/Props';
import MoveSeriesModal from 'Series/MoveSeries/MoveSeriesModal';
import translate from 'Utilities/String/translate';
import styles from './EditSeriesModalContent.css';

class EditSeriesModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isConfirmMoveModalOpen: false
    };
  }

  //
  // Listeners

  onSavePress = () => {
    const {
      isPathChanging,
      onSavePress
    } = this.props;

    if (isPathChanging && !this.state.isConfirmMoveModalOpen) {
      this.setState({ isConfirmMoveModalOpen: true });
    } else {
      this.setState({ isConfirmMoveModalOpen: false });

      onSavePress(false);
    }
  };

  onMoveSeriesPress = () => {
    this.setState({ isConfirmMoveModalOpen: false });

    this.props.onSavePress(true);
  };

  //
  // Render

  render() {
    const {
      seriesName,
      item,
      isSaving,
      originalPath,
      overview,
      year,
      isOverridden,
      overriddenFields,
      metadataOverrides,
      onInputChange,
      onMetadataChange,
      onModalClose,
      onDeleteSeriesPress,
      ...otherProps
    } = this.props;

    const {
      monitored,
      monitorNewItems,
      qualityProfileId,
      path,
      tags
    } = item;

    const overridden = (overriddenFields || '').split(',').filter(Boolean);

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          Edit - {seriesName}
        </ModalHeader>

        <ModalBody>
          <Form {...otherProps}>
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
                {translate('MonitorNewItems')}
                <Popover
                  anchor={
                    <Icon
                      className={styles.labelIcon}
                      name={icons.INFO}
                    />
                  }
                  title={translate('MonitorNewItems')}
                  body={<SeriesMonitorNewItemsOptionsPopoverContent />}
                  position={tooltipPositions.RIGHT}
                />
              </FormLabel>

              <FormInputGroup
                type={inputTypes.MONITOR_NEW_ITEMS_SELECT}
                name="monitorNewItems"
                helpText={translate('MonitorNewItemsHelpText')}
                {...monitorNewItems}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('QualityProfile')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.QUALITY_PROFILE_SELECT}
                name="qualityProfileId"
                {...qualityProfileId}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('Path')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.PATH}
                name="path"
                {...path}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>
                {translate('Tags')}
              </FormLabel>

              <FormInputGroup
                type={inputTypes.TAG}
                name="tags"
                {...tags}
                onChange={onInputChange}
              />
            </FormGroup>

            <div className={styles.metadataSection}>
              <div className={styles.metadataHeader}>
                Metadata Overrides
                {isOverridden &&
                  <span className={styles.overriddenBadge}>
                    {overridden.length} field(s) overridden
                  </span>
                }
              </div>

              <FormGroup>
                <FormLabel>
                  Series Name
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="seriesName"
                  value={metadataOverrides.seriesName !== undefined ? metadataOverrides.seriesName : seriesName}
                  helpText={overridden.includes('Name') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Year
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.NUMBER}
                  name="year"
                  value={metadataOverrides.year !== undefined ? metadataOverrides.year : (year || '')}
                  helpText={overridden.includes('Year') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>
                  Overview
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="overview"
                  value={metadataOverrides.overview !== undefined ? metadataOverrides.overview : (overview || '')}
                  helpText={overridden.includes('Overview') ? 'Currently overridden' : 'Change to override provider metadata'}
                  onChange={onMetadataChange}
                />
              </FormGroup>
            </div>
          </Form>
        </ModalBody>
        <ModalFooter>
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteSeriesPress}
          >
            Delete
          </Button>

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

        <MoveSeriesModal
          originalPath={originalPath}
          destinationPath={path.value}
          isOpen={this.state.isConfirmMoveModalOpen}
          onSavePress={this.onSavePress}
          onMoveSeriesPress={this.onMoveSeriesPress}
        />

      </ModalContent>
    );
  }
}

EditSeriesModalContent.propTypes = {
  seriesId: PropTypes.number.isRequired,
  seriesName: PropTypes.string.isRequired,
  item: PropTypes.object.isRequired,
  isSaving: PropTypes.bool.isRequired,
  isPathChanging: PropTypes.bool.isRequired,
  originalPath: PropTypes.string.isRequired,
  overview: PropTypes.string,
  year: PropTypes.number,
  isOverridden: PropTypes.bool,
  overriddenFields: PropTypes.string,
  metadataOverrides: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onMetadataChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onDeleteSeriesPress: PropTypes.func.isRequired
};

export default EditSeriesModalContent;
