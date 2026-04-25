import moment from 'moment';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddNewIssueModal from './AddNewIssueModal';
import styles from './AddNewIssueSearchResult.css';

class AddNewIssueSearchResult extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNewAddIssueModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (!prevProps.isExistingIssue && this.props.isExistingIssue) {
      this.onAddIssueModalClose();
    }
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddIssueModalOpen: true });
  };

  onAddIssueModalClose = () => {
    this.setState({ isNewAddIssueModalOpen: false });
  };

  onMBLinkPress = (event) => {
    event.stopPropagation();
  };

  //
  // Render

  render() {
    const {
      foreignIssueId,
      titleSlug,
      title,
      seriesTitle,
      releaseDate,
      disambiguation,
      overview,
      ratings,
      images,
      series,
      editions,
      isExistingIssue,
      isExistingSeries,
      isSmallScreen
    } = this.props;

    const {
      isNewAddIssueModalOpen
    } = this.state;

    const linkProps = isExistingIssue ? { to: `/issue/${titleSlug}` } : { onPress: this.onPress };

    // Get poster URL from images
    let posterUrl = null;
    if (images && images.length > 0) {
      const posterImage = images.find((img) => img.coverType === 'cover' || img.coverType === 'poster');
      if (posterImage) {
        posterUrl = posterImage.remoteUrl || posterImage.url;
      }
    }

    const overviewText = overview ? stripHtml(overview) : null;
    const yearText = releaseDate ? moment(releaseDate).format('YYYY') : null;

    return (
      <div className={styles.searchResult}>
        <Link
          className={styles.underlay}
          {...linkProps}
        />

        <div className={styles.overlay}>
          <div className={styles.posterContainer}>
            {
              posterUrl ?
                <img
                  className={styles.poster}
                  src={posterUrl}
                  alt={title}
                /> :
                <div className={styles.posterPlaceholder}>
                  <Icon
                    name={icons.MISSING}
                    size={24}
                  />
                </div>
            }
          </div>

          <div className={styles.content}>
            <div className={styles.title}>
              {title}

              {
                !!disambiguation &&
                  <span className={styles.year}>({disambiguation})</span>
              }
            </div>

            {
              seriesTitle ?
                <div className={styles.series}>
                  {seriesTitle}
                </div> :
                null
            }

            <div className={styles.metaRow}>
              {
                yearText ?
                  <span>{yearText}</span> :
                  null
              }

              {
                yearText && ratings && ratings.votes > 0 ?
                  <span className={styles.metaSeparator} /> :
                  null
              }

              {
                ratings && ratings.votes > 0 ?
                  <span>
                    Rating: {ratings.value.toFixed(1)}
                  </span> :
                  null
              }
            </div>

            {
              overviewText ?
                <div className={styles.overview}>
                  {overviewText}
                </div> :
                null
            }
          </div>

          <div className={styles.icons}>
            {
              isExistingIssue ?
                <Icon
                  className={styles.alreadyExistsIcon}
                  name={icons.CHECK_CIRCLE}
                  size={22}
                  title={translate('AlreadyInYourLibrary')}
                /> :
                null
            }

            {
              editions && editions.length > 0 ?
                <Link
                  className={styles.mbLink}
                  to={`https://metron.cloud/issue/${editions[0].foreignEditionId}/`}
                  onPress={this.onMBLinkPress}
                >
                  <Icon
                    className={styles.mbLinkIcon}
                    name={icons.EXTERNAL_LINK}
                    size={18}
                  />
                </Link> : null
            }
          </div>
        </div>

        <AddNewIssueModal
          isOpen={isNewAddIssueModalOpen && !isExistingIssue}
          isExistingSeries={isExistingSeries}
          foreignIssueId={foreignIssueId}
          issueTitle={title}
          seriesTitle={seriesTitle}
          disambiguation={disambiguation}
          seriesName={series.seriesName}
          overview={overview}
          folder={series.folder}
          images={images}
          onModalClose={this.onAddIssueModalClose}
        />
      </div>
    );
  }
}

AddNewIssueSearchResult.propTypes = {
  foreignIssueId: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string,
  releaseDate: PropTypes.string,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  series: PropTypes.object,
  editions: PropTypes.arrayOf(PropTypes.object),
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isExistingIssue: PropTypes.bool.isRequired,
  isExistingSeries: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired
};

export default AddNewIssueSearchResult;
