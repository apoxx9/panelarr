import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SelectInput from 'Components/Form/SelectInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import IssueEditorFooterLabel from './IssueEditorFooterLabel';
import DeleteIssueModal from './Delete/DeleteIssueModal';
import styles from './IssueEditorFooter.css';

const NO_CHANGE = 'noChange';

class IssueEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      monitored: NO_CHANGE,
      rootFolderPath: NO_CHANGE,
      savingTags: false,
      isDeleteIssueModalOpen: false,
      isTagsModalOpen: false,
      isConfirmMoveModalOpen: false,
      destinationRootFolder: null
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        monitored: NO_CHANGE,
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
      case 'monitored':
        this.props.onSaveSelected({ [name]: value === 'monitored' });
        break;
      default:
        this.props.onSaveSelected({ [name]: value });
    }
  };

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteIssueModalOpen: true });
  };

  onDeleteIssueModalClose = () => {
    this.setState({ isDeleteIssueModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      issueIds,
      selectedCount,
      isSaving,
      isDeleting
    } = this.props;

    const {
      monitored,
      isDeleteIssueModalOpen
    } = this.state;

    const monitoredOptions = [
      { key: NO_CHANGE, value: translate('NoChange'), isDisabled: true },
      { key: 'monitored', value: translate('Monitored') },
      { key: 'unmonitored', value: translate('Unmonitored') }
    ];

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <IssueEditorFooterLabel
            label={translate('MonitorIssue')}
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

        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <IssueEditorFooterLabel
              label={translate('SelectedCountIssuesSelectedInterp', [selectedCount])}
              isSaving={false}
            />

            <div className={styles.buttons}>
              <SpinnerButton
                className={styles.deleteSelectedButton}
                kind={kinds.DANGER}
                isSpinning={isDeleting}
                isDisabled={!selectedCount || isDeleting}
                onPress={this.onDeleteSelectedPress}
              >
                Delete
              </SpinnerButton>
            </div>
          </div>
        </div>

        <DeleteIssueModal
          isOpen={isDeleteIssueModalOpen}
          issueIds={issueIds}
          onModalClose={this.onDeleteIssueModalClose}
        />

      </PageContentFooter>
    );
  }
}

IssueEditorFooter.propTypes = {
  issueIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSaveSelected: PropTypes.func.isRequired
};

export default IssueEditorFooter;
