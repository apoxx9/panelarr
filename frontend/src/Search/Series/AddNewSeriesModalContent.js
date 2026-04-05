import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import SeriesPoster from 'Series/SeriesPoster';
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
import styles from './AddNewSeriesModalContent.css';

class AddNewSeriesModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchForMissingIssues: false
    };
  }

  //
  // Listeners

  onSearchForMissingIssuesChange = ({ value }) => {
    this.setState({ searchForMissingIssues: value });
  };

  onAddSeriesPress = () => {
    this.props.onAddSeriesPress(this.state.searchForMissingIssues);
  };

  //
  // Render

  render() {
    const {
      seriesName,
      disambiguation,
      overview,
      images,
      isAdding,
      isSmallScreen,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewSeries')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <SeriesPoster
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {seriesName}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

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

              <AddSeriesOptionsForm
                {...otherProps}
              />

            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForMissingIssuesLabelContainer}>
            <span className={styles.searchForMissingIssuesLabel}>
              Start search for missing issues
            </span>

            <CheckInput
              containerClassName={styles.searchForMissingIssuesContainer}
              className={styles.searchForMissingIssuesInput}
              name="searchForMissingIssues"
              value={this.state.searchForMissingIssues}
              onChange={this.onSearchForMissingIssuesChange}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddSeriesPress}
          >
            Add {seriesName}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewSeriesModalContent.propTypes = {
  seriesName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  isSmallScreen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onAddSeriesPress: PropTypes.func.isRequired
};

export default AddNewSeriesModalContent;
