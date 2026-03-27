import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddNewSeriesModal from './AddNewSeriesModal';
import styles from './AddNewSeriesSearchResult.css';

class AddNewSeriesSearchResult extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNewAddSeriesModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (!prevProps.isExistingSeries && this.props.isExistingSeries) {
      this.onAddSeriesModalClose();
    }
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddSeriesModalOpen: true });
  };

  onAddSeriesModalClose = () => {
    this.setState({ isNewAddSeriesModalOpen: false });
  };

  onMBLinkPress = (event) => {
    event.stopPropagation();
  };

  //
  // Render

  render() {
    const {
      foreignSeriesId,
      titleSlug,
      seriesName,
      year,
      disambiguation,
      status,
      overview,
      ratings,
      folder,
      images,
      remotePoster,
      isExistingSeries,
      isSmallScreen
    } = this.props;

    const {
      isNewAddSeriesModalOpen
    } = this.state;

    const linkProps = isExistingSeries ? { to: `/series/${titleSlug}` } : { onPress: this.onPress };

    // Extract year from folder name if year prop is not available
    // Folder format is typically "Series Name (YYYY)"
    let displayYear = year;
    if (!displayYear && folder) {
      const match = folder.match(/\((\d{4})\)/);
      if (match) {
        displayYear = parseInt(match[1]);
      }
    }

    const statusLabel = status === 'ended' ? 'Ended' : 'Continuing';
    const statusClass = status === 'ended' ? styles.statusEnded : styles.statusContinuing;

    // Get poster URL - prefer remotePoster, fall back to image array
    let posterUrl = remotePoster;
    if (!posterUrl && images && images.length > 0) {
      const posterImage = images.find((img) => img.coverType === 'poster');
      if (posterImage) {
        posterUrl = posterImage.remoteUrl || posterImage.url;
      }
    }

    const overviewText = overview ? stripHtml(overview) : null;

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
                  alt={seriesName}
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
            <div className={styles.name}>
              {seriesName}

              {
                displayYear && (!seriesName.includes || !seriesName.includes(String(displayYear))) ?
                  <span className={styles.year}>
                    ({displayYear})
                  </span> :
                  null
              }

              {
                !!disambiguation &&
                  <span className={styles.year}>({disambiguation})</span>
              }
            </div>

            <div className={styles.metaRow}>
              <span className={statusClass}>
                {statusLabel}
              </span>

              {
                displayYear ?
                  <>
                    <span className={styles.metaSeparator} />
                    <span>{displayYear}</span>
                  </> :
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
              isExistingSeries ?
                <Icon
                  className={styles.alreadyExistsIcon}
                  name={icons.CHECK_CIRCLE}
                  size={22}
                  title={translate('AlreadyInYourLibrary')}
                /> :
                null
            }

            <Link
              className={styles.mbLink}
              to={`https://metron.cloud/series/${foreignSeriesId}/`}
              onPress={this.onMBLinkPress}
            >
              <Icon
                className={styles.mbLinkIcon}
                name={icons.EXTERNAL_LINK}
                size={18}
              />
            </Link>
          </div>
        </div>

        <AddNewSeriesModal
          isOpen={isNewAddSeriesModalOpen && !isExistingSeries}
          foreignSeriesId={foreignSeriesId}
          seriesName={seriesName}
          disambiguation={disambiguation}
          year={year}
          overview={overview}
          folder={folder}
          images={images}
          onModalClose={this.onAddSeriesModalClose}
        />
      </div>
    );
  }
}

AddNewSeriesSearchResult.propTypes = {
  foreignSeriesId: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  year: PropTypes.number,
  disambiguation: PropTypes.string,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  folder: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  remotePoster: PropTypes.string,
  isExistingSeries: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired
};

export default AddNewSeriesSearchResult;
