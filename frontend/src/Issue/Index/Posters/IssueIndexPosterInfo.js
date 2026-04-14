import PropTypes from 'prop-types';
import React from 'react';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import styles from './IssueIndexPosterInfo.css';

function IssueIndexPosterInfo(props) {
  const {
    qualityProfile,
    showQualityProfile,
    added,
    releaseDate,
    series,
    issueFileCount,
    sizeOnDisk,
    sortKey,
    showRelativeDates,
    shortDateFormat,
    timeFormat
  } = props;

  if (sortKey === 'qualityProfileId' && !showQualityProfile) {
    return (
      <div className={styles.info}>
        {qualityProfile.name}
      </div>
    );
  }

  if (sortKey === 'added' && added) {
    const addedDate = getRelativeDate(
      added,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Added ${addedDate}`}
      </div>
    );
  }

  if (sortKey === 'releaseDate' && added) {
    const date = getRelativeDate(
      releaseDate,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Released ${date}`}
      </div>
    );
  }

  if (sortKey === 'issueFileCount') {
    let issues = '1 file';

    if (issueFileCount === 0) {
      issues = 'No files';
    } else if (issueFileCount > 1) {
      issues = `${issueFileCount} files`;
    }

    return (
      <div className={styles.info}>
        {issues}
      </div>
    );
  }

  if (sortKey === 'path') {
    return (
      <div className={styles.info}>
        {series.path}
      </div>
    );
  }

  if (sortKey === 'sizeOnDisk') {
    return (
      <div className={styles.info}>
        {formatBytes(sizeOnDisk)}
      </div>
    );
  }

  return null;
}

IssueIndexPosterInfo.propTypes = {
  qualityProfile: PropTypes.object.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  series: PropTypes.object.isRequired,
  added: PropTypes.string,
  releaseDate: PropTypes.string,
  issueFileCount: PropTypes.number.isRequired,
  sizeOnDisk: PropTypes.number,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default IssueIndexPosterInfo;
