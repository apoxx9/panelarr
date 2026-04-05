import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import IssueCover from 'Issue/IssueCover';
import CheckInput from 'Components/Form/CheckInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddSeriesOptionsForm from '../Common/AddSeriesOptionsForm.js';
import styles from './AddNewIssueModalContent.css';

class AddNewIssueModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchForNewIssue: false
    };
  }

  //
  // Listeners

  onSearchForNewIssueChange = ({ value }) => {
    this.setState({ searchForNewIssue: value });
  };

  onAddIssuePress = () => {
    this.props.onAddIssuePress(this.state.searchForNewIssue);
  };

  //
  // Render

  render() {
    const {
      issueTitle,
      seriesTitle,
      seriesName,
      disambiguation,
      overview,
      images,
      isAdding,
      isExistingSeries,
      isSmallScreen,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewIssue')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <IssueCover
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {issueTitle}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

              {
                !!seriesTitle &&
                  <div className={styles.series}>
                    {seriesTitle}
                  </div>
              }

              <div>
                <span className={styles.seriesName}> By: {seriesName}</span>
              </div>

              {
                overview ?
                  <div className={styles.overview}>
                    <TextTruncate
                      truncateText="…"
                      line={8}
                      text={stripHtml(overview)}
                    />
                  </div> :
                  null
              }

              {
                !isExistingSeries &&
                  <AddSeriesOptionsForm
                    seriesName={seriesName}
                    includeSpecificIssueMonitor={true}
                    {...otherProps}
                  />
              }
            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForNewIssueLabelContainer}>
            <span className={styles.searchForNewIssueLabel}>
              Start search for new issue
            </span>

            <CheckInput
              containerClassName={styles.searchForNewIssueContainer}
              className={styles.searchForNewIssueInput}
              name="searchForNewIssue"
              value={this.state.searchForNewIssue}
              onChange={this.onSearchForNewIssueChange}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddIssuePress}
          >
            Add {issueTitle}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewIssueModalContent.propTypes = {
  issueTitle: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string,
  seriesName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  isExistingSeries: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onAddIssuePress: PropTypes.func.isRequired
};

export default AddNewIssueModalContent;
