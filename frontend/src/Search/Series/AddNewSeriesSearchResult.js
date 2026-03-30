import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddNewSeriesModal from './AddNewSeriesModal';
import styles from './AddNewSeriesSearchResult.css';

function getExternalLink(foreignSeriesId) {
  if (foreignSeriesId && foreignSeriesId.startsWith('cv:')) {
    const cvId = foreignSeriesId.replace('cv:', '');
    return `https://comicvine.gamespot.com/volume/4050-${cvId}/`;
  }

  return `https://metron.cloud/series/${foreignSeriesId}/`;
}

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

  onExternalLinkPress = (event) => {
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
      seriesType,
      volumeNumber,
      overview,
      ratings,
      folder,
      images,
      remotePoster,
      statistics,
      isExistingSeries,
      isSmallScreen
    } = this.props;

    // Parse disambiguation: "Publisher|IssueCount" or just "Publisher"
    let publisherName = null;
    let providerIssueCount = 0;

    if (disambiguation) {
      const parts = disambiguation.split('|');
      publisherName = parts[0] || null;
      if (parts[1]) {
        providerIssueCount = parseInt(parts[1]) || 0;
      }
    }

    // Issue count: prefer statistics (from DB), fall back to provider data
    const issueCount = (statistics && statistics.issueCount > 0) ? statistics.issueCount : providerIssueCount;

    const {
      isNewAddSeriesModalOpen
    } = this.state;

    const linkProps = isExistingSeries ? { to: `/series/${titleSlug}` } : { onPress: this.onPress };

    // Extract year from folder name or series name if year prop is not available
    // Folder format: "Series Name (YYYY)" or series name includes "(YYYY)"
    let displayYear = year;

    if (!displayYear && folder) {
      const match = folder.match(/\((\d{4})\)/);

      if (match) {
        displayYear = parseInt(match[1]);
      }
    }

    if (!displayYear && seriesName) {
      const match = seriesName.match(/\((\d{4})\)/);

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
    const externalLink = getExternalLink(foreignSeriesId);

    // Build a clean display name: strip year from name if already shown separately
    let displayName = seriesName;

    if (displayYear && seriesName.includes(`(${displayYear})`)) {
      displayName = seriesName.replace(`(${displayYear})`, '').trim();
    }

    return (
      <div className={styles.searchResult}>
        <Link
          className={styles.underlay}
          {...linkProps}
        />

        <div className={styles.overlay}>
          {
            isExistingSeries ?
              <div className={styles.existsBadge}>
                <Icon
                  name={icons.CHECK_CIRCLE}
                  size={14}
                />
              </div> :
              null
          }

          <div className={styles.posterContainer}>
            {
              posterUrl ?
                <img
                  className={styles.poster}
                  src={posterUrl}
                  alt={seriesName}
                  loading="lazy"
                /> :
                <div className={styles.posterPlaceholder}>
                  <span className={styles.placeholderLetter}>
                    {seriesName ? seriesName.charAt(0).toUpperCase() : '?'}
                  </span>
                </div>
            }
          </div>

          <div className={styles.content}>
            <div className={styles.titleRow}>
              <div className={styles.name}>
                {displayName}

                {
                  displayYear ?
                    <span className={styles.year}>
                      ({displayYear})
                    </span> :
                    null
                }

                {
                  !!volumeNumber &&
                    <span className={styles.volumeBadge}>V{volumeNumber}</span>
                }
              </div>
            </div>

            <div className={styles.metaRow}>
              {
                !!publisherName &&
                  <span className={styles.publisherBadge}>
                    {publisherName}
                  </span>
              }

              {
                issueCount > 0 ?
                  <span>{issueCount} {issueCount === 1 ? 'issue' : 'issues'}</span> :
                  null
              }

              {
                !!seriesType &&
                  <span className={styles.typeBadge}>
                    {seriesType}
                  </span>
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
              !isExistingSeries &&
                <Link
                  className={styles.addButton}
                  onPress={this.onPress}
                >
                  <Icon
                    name={icons.ADD}
                    size={18}
                  />
                </Link>
            }

            <Link
              className={styles.externalLink}
              to={externalLink}
              onPress={this.onExternalLinkPress}
            >
              <Icon
                className={styles.externalLinkIcon}
                name={icons.EXTERNAL_LINK}
                size={16}
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
  seriesType: PropTypes.string,
  volumeNumber: PropTypes.number,
  overview: PropTypes.string,
  ratings: PropTypes.object.isRequired,
  folder: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  remotePoster: PropTypes.string,
  statistics: PropTypes.object,
  isExistingSeries: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired
};

export default AddNewSeriesSearchResult;
