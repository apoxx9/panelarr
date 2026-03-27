import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesMetadataProfilePopoverContent from 'AddSeries/SeriesMetadataProfilePopoverContent';
import SeriesMonitoringOptionsPopoverContent from 'AddSeries/SeriesMonitoringOptionsPopoverContent';
import SeriesMonitorNewItemsOptionsPopoverContent from 'AddSeries/SeriesMonitorNewItemsOptionsPopoverContent';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Popover from 'Components/Tooltip/Popover';
import { icons, inputTypes, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './AddSeriesOptionsForm.css';

class AddSeriesOptionsForm extends Component {

  //
  // Listeners

  onQualityProfileIdChange = ({ value }) => {
    this.props.onInputChange({ name: 'qualityProfileId', value: parseInt(value) });
  };

  onMetadataProfileIdChange = ({ value }) => {
    this.props.onInputChange({ name: 'metadataProfileId', value: parseInt(value) });
  };

  //
  // Render

  render() {
    const {
      rootFolderPath,
      monitor,
      monitorNewItems,
      qualityProfileId,
      metadataProfileId,
      includeNoneMetadataProfile,
      includeSpecificIssueMonitor,
      showMetadataProfile,
      folder,
      tags,
      isWindows,
      onInputChange,
      ...otherProps
    } = this.props;

    return (
      <Form {...otherProps}>
        <FormGroup>
          <FormLabel>
            {translate('RootFolder')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.ROOT_FOLDER_SELECT}
            name="rootFolderPath"
            valueOptions={{
              seriesFolder: folder,
              isWindows
            }}
            selectedValueOptions={{
              seriesFolder: folder,
              isWindows
            }}
            helpText={translate('AddNewSeriesRootFolderHelpText', { folder })}
            onChange={onInputChange}
            {...rootFolderPath}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {translate('Monitor')}

            <Popover
              anchor={
                <Icon
                  className={styles.labelIcon}
                  name={icons.INFO}
                />
              }
              title={translate('MonitoringOptions')}
              body={<SeriesMonitoringOptionsPopoverContent />}
              position={tooltipPositions.RIGHT}
            />
          </FormLabel>

          <FormInputGroup
            type={inputTypes.MONITOR_ISSUES_LIST_SELECT}
            name="monitor"
            helpText={translate('MonitoringOptionsHelpText')}
            onChange={onInputChange}
            includeSpecificIssue={includeSpecificIssueMonitor}
            {...monitor}
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
            onChange={this.onQualityProfileIdChange}
            {...qualityProfileId}
          />
        </FormGroup>

        <FormGroup className={showMetadataProfile ? undefined : styles.hideMetadataProfile}>
          <FormLabel>
            {translate('MetadataProfile')}

            {
              includeNoneMetadataProfile &&
                <Popover
                  anchor={
                    <Icon
                      className={styles.labelIcon}
                      name={icons.INFO}
                    />
                  }
                  title={translate('MetadataProfile')}
                  body={<SeriesMetadataProfilePopoverContent />}
                  position={tooltipPositions.RIGHT}
                />
            }
          </FormLabel>

          <FormInputGroup
            type={inputTypes.METADATA_PROFILE_SELECT}
            name="metadataProfileId"
            includeNone={includeNoneMetadataProfile}
            onChange={this.onMetadataProfileIdChange}
            {...metadataProfileId}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>
            {translate('Tags')}
          </FormLabel>

          <FormInputGroup
            type={inputTypes.TAG}
            name="tags"
            onChange={onInputChange}
            {...tags}
          />
        </FormGroup>
      </Form>
    );
  }
}

AddSeriesOptionsForm.propTypes = {
  rootFolderPath: PropTypes.object,
  monitor: PropTypes.object.isRequired,
  monitorNewItems: PropTypes.object.isRequired,
  qualityProfileId: PropTypes.object,
  metadataProfileId: PropTypes.object,
  showMetadataProfile: PropTypes.bool.isRequired,
  includeNoneMetadataProfile: PropTypes.bool.isRequired,
  includeSpecificIssueMonitor: PropTypes.bool.isRequired,
  folder: PropTypes.string.isRequired,
  tags: PropTypes.object.isRequired,
  isWindows: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

AddSeriesOptionsForm.defaultProps = {
  includeSpecificIssueMonitor: false
};

export default AddSeriesOptionsForm;
