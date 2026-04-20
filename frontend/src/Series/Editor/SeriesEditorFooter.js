import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import MoveSeriesModal from 'Series/MoveSeries/MoveSeriesModal';
import MonitorNewItemsSelectInput from 'Components/Form/MonitorNewItemsSelectInput';
import QualityProfileSelectInputConnector from 'Components/Form/QualityProfileSelectInputConnector';
import RootFolderSelectInputConnector from 'Components/Form/RootFolderSelectInputConnector';
import SelectInput from 'Components/Form/SelectInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import { fetchRootFolders } from 'Store/Actions/Settings/rootFolders';
import translate from 'Utilities/String/translate';
import SeriesEditorFooterLabel from './SeriesEditorFooterLabel';
import DeleteSeriesModal from './Delete/DeleteSeriesModal';
import TagsModal from './Tags/TagsModal';
import styles from './SeriesEditorFooter.css';

const NO_CHANGE = 'noChange';

const mapDispatchToProps = {
  dispatchFetchRootFolders: fetchRootFolders
};

class SeriesEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitored: NO_CHANGE,
      monitorNewItems: NO_CHANGE,
      qualityProfileId: NO_CHANGE,
      rootFolderPath: NO_CHANGE,
      savingTags: false,
      isDeleteSeriesModalOpen: false,
      isTagsModalOpen: false,
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    };
  }

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchRootFolders();
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        monitored: NO_CHANGE,
        monitorNewItems: NO_CHANGE,
        qualityProfileId: NO_CHANGE,
        rootFolderPath: NO_CHANGE,
        savingTags: false
      });
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });

    if (value === NO_CHANGE) {
      return;
    }

    switch (name) {
      case 'rootFolderPath':
        this.setState({
          isConfirmMoveModalOpen: true,
          destinationRootFolder: value
        });
        break;
      case 'monitored':
        this.props.onSaveSelected({ [name]: value === 'monitored' });
        break;
      default:
        this.props.onSaveSelected({ [name]: value });
    }
  };

  onApplyTagsPress = (tags, applyTags) => {
    this.setState({
      savingTags: true,
      isTagsModalOpen: false
    });

    this.props.onSaveSelected({
      tags,
      applyTags
    });
  };

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteSeriesModalOpen: true });
  };

  onDeleteSeriesModalClose = () => {
    this.setState({ isDeleteSeriesModalOpen: false });
  };

  onTagsPress = () => {
    this.setState({ isTagsModalOpen: true });
  };

  onTagsModalClose = () => {
    this.setState({ isTagsModalOpen: false });
  };

  onSaveRootFolderPress = () => {
    this.setState({
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    });

    this.props.onSaveSelected({ rootFolderPath: this.state.destinationRootFolder });
  };

  onMoveSeriesPress = () => {
    this.setState({
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    });

    this.props.onSaveSelected({
      rootFolderPath: this.state.destinationRootFolder,
      moveFiles: true
    });
  };

  //
  // Render

  render() {
    const {
      seriesIds,
      selectedCount,
      isSaving,
      isDeleting,
      isOrganizingSeries,
      isRetaggingSeries,
      onOrganizeSeriesPress,
      onRetagSeriesPress
    } = this.props;

    const {
      monitored,
      monitorNewItems,
      qualityProfileId,
      rootFolderPath,
      savingTags,
      isTagsModalOpen,
      isDeleteSeriesModalOpen,
      isConfirmMoveModalOpen,
      destinationRootFolder
    } = this.state;

    const monitoredOptions = [
      { key: NO_CHANGE, value: translate('NoChange'), isDisabled: true },
      { key: 'monitored', value: translate('Monitored') },
      { key: 'unmonitored', value: translate('Unmonitored') }
    ];

    return (
      <PageContentFooter>
        <div className={styles.footer}>
          <div className={styles.dropdownContainer}>
            <div className={styles.inputContainer}>
              <SeriesEditorFooterLabel
                label={translate('MonitorSeries')}
                isSaving={isSaving && monitored !== NO_CHANGE}
              />

              <SelectInput
                name="monitored"
                value={monitored}
                values={monitoredOptions}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div className={styles.inputContainer}>
              <SeriesEditorFooterLabel
                label={translate('MonitorNewItems')}
                isSaving={isSaving && monitored !== NO_CHANGE}
              />

              <MonitorNewItemsSelectInput
                name="monitorNewItems"
                value={monitorNewItems}
                includeNoChange={true}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div className={styles.inputContainer}>
              <SeriesEditorFooterLabel
                label={translate('QualityProfile')}
                isSaving={isSaving && qualityProfileId !== NO_CHANGE}
              />

              <QualityProfileSelectInputConnector
                name="qualityProfileId"
                value={qualityProfileId}
                includeNoChange={true}
                isDisabled={!selectedCount}
                onChange={this.onInputChange}
              />
            </div>

            <div
              className={styles.inputContainer}
            >
              <SeriesEditorFooterLabel
                label={translate('RootFolder')}
                isSaving={isSaving && rootFolderPath !== NO_CHANGE}
              />

              <RootFolderSelectInputConnector
                name="rootFolderPath"
                value={rootFolderPath}
                includeNoChange={true}
                isDisabled={!selectedCount}
                selectedValueOptions={{ includeFreeSpace: false }}
                onChange={this.onInputChange}
              />
            </div>
          </div>

          <div className={styles.buttonContainer}>
            <div className={styles.buttonContainerContent}>
              <SeriesEditorFooterLabel
                label={translate('SelectedCountSeriesSelectedInterp', [selectedCount])}
                isSaving={false}
              />

              <div className={styles.buttons}>

                <SpinnerButton
                  className={styles.organizeSelectedButton}
                  kind={kinds.WARNING}
                  isSpinning={isOrganizingSeries}
                  isDisabled={!selectedCount || isOrganizingSeries || isRetaggingSeries}
                  onPress={onOrganizeSeriesPress}
                >
                  {translate('RenameFiles')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.organizeSelectedButton}
                  kind={kinds.WARNING}
                  isSpinning={isRetaggingSeries}
                  isDisabled={!selectedCount || isOrganizingSeries || isRetaggingSeries}
                  onPress={onRetagSeriesPress}
                >
                  {translate('WriteMetadataTags')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.tagsButton}
                  isSpinning={isSaving && savingTags}
                  isDisabled={!selectedCount || isOrganizingSeries || isRetaggingSeries}
                  onPress={this.onTagsPress}
                >
                  {translate('SetPanelarrTags')}
                </SpinnerButton>

                <SpinnerButton
                  className={styles.deleteSelectedButton}
                  kind={kinds.DANGER}
                  isSpinning={isDeleting}
                  isDisabled={!selectedCount || isDeleting}
                  onPress={this.onDeleteSelectedPress}
                >
                  {translate('Delete')}
                </SpinnerButton>

              </div>
            </div>
          </div>
        </div>

        <TagsModal
          isOpen={isTagsModalOpen}
          seriesIds={seriesIds}
          onApplyTagsPress={this.onApplyTagsPress}
          onModalClose={this.onTagsModalClose}
        />

        <DeleteSeriesModal
          isOpen={isDeleteSeriesModalOpen}
          seriesIds={seriesIds}
          onModalClose={this.onDeleteSeriesModalClose}
        />

        <MoveSeriesModal
          destinationRootFolder={destinationRootFolder}
          isOpen={isConfirmMoveModalOpen}
          onSavePress={this.onSaveRootFolderPress}
          onMoveSeriesPress={this.onMoveSeriesPress}
        />

      </PageContentFooter>
    );
  }
}

SeriesEditorFooter.propTypes = {
  seriesIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  isOrganizingSeries: PropTypes.bool.isRequired,
  isRetaggingSeries: PropTypes.bool.isRequired,
  onSaveSelected: PropTypes.func.isRequired,
  onOrganizeSeriesPress: PropTypes.func.isRequired,
  onRetagSeriesPress: PropTypes.func.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired
};

export default connect(undefined, mapDispatchToProps)(SeriesEditorFooter);
