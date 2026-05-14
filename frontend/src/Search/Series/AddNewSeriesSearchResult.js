import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
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
      overview,
      folder,
      images,
      remotePoster,
      statistics,
      isExistingSeries
    } = this.props;

    // Parse disambiguation: "Publisher|IssueCount" or just "Publisher"
    let publisherName = '';
    let providerIssueCount = 0;

    if (disambiguation) {
      const parts = disambiguation.split('|');
      publisherName = parts[0] || '';
      if (parts[1]) {
        providerIssueCount = parseInt(parts[1]) || 0;
      }
    }

    const issueCount = (statistics && statistics.issueCount > 0) ? statistics.issueCount : providerIssueCount;

    const {
      isNewAddSeriesModalOpen
    } = this.state;

    const statusMap = {
      continuing: { label: 'Continuing', class: styles.statusContinuing },
      ended: { label: 'Ended', class: styles.statusEnded },
      cancelled: { label: 'Cancelled', class: styles.statusCancelled },
      hiatus: { label: 'Hiatus', class: styles.statusCancelled }
    };
    const statusInfo = statusMap[(status || '').toLowerCase()] || { label: '—', class: styles.statusUnknown };
    const statusLabel = statusInfo.label;
    const statusClass = statusInfo.class;

    // Get poster URL
    let posterUrl = remotePoster;

    if (!posterUrl && images && images.length > 0) {
      const posterImage = images.find((img) => img.coverType === 'poster');

      if (posterImage) {
        posterUrl = posterImage.remoteUrl || posterImage.url;
      }
    }

    const externalLink = getExternalLink(foreignSeriesId);

    return (
      <tr className={isExistingSeries ? styles.existingRow : styles.row}>
        <td className={styles.posterCell}>
          {
            posterUrl ?
              <img
                className={styles.thumbnail}
                src={posterUrl}
                alt={seriesName}
                loading="lazy"
              /> :
              <div className={styles.thumbnailPlaceholder}>
                {seriesName ? seriesName.charAt(0).toUpperCase() : '?'}
              </div>
          }
        </td>

        <td className={styles.nameCell}>
          {
            isExistingSeries ?
              <Link to={`/series/${titleSlug}`} className={styles.seriesLink}>
                {seriesName}
              </Link> :
              <Link onPress={this.onPress} className={styles.seriesLink}>
                {seriesName}
              </Link>
          }
          {
            isExistingSeries &&
              <Icon
                className={styles.existsIcon}
                name={icons.CHECK_CIRCLE}
                size={12}
                title="Already in library"
              />
          }
        </td>

        <td className={styles.yearCell}>
          {year || '—'}
        </td>

        <td className={styles.publisherCell}>
          {publisherName || '—'}
        </td>

        <td className={styles.issueCountCell}>
          {issueCount || '—'}
        </td>

        <td className={styles.statusCell}>
          <span className={statusClass}>
            {statusLabel}
          </span>
        </td>

        <td className={styles.actionCell}>
          {
            !isExistingSeries &&
              <Link
                className={styles.addButton}
                onPress={this.onPress}
                title="Add Series"
              >
                <Icon
                  name={icons.ADD}
                  size={16}
                />
              </Link>
          }

          <Link
            className={styles.externalLink}
            to={externalLink}
            onPress={this.onExternalLinkPress}
            title="View on metadata provider"
          >
            <Icon
              name={icons.EXTERNAL_LINK}
              size={14}
            />
          </Link>
        </td>

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
      </tr>
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
