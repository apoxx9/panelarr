import PropTypes from 'prop-types';
import React from 'react';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import styles from './SeriesIndexPosterInfo.css';

function SeriesIndexPosterInfo(props) {
  const {
    qualityProfile,
    showQualityProfile,
    added,
    nextIssue,
    lastIssue,
    issueCount,
    path,
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

  if (sortKey === 'nextIssue' && nextIssue) {
    const date = getRelativeDate(
      nextIssue.releaseDate,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Next Issue ${date}`}
      </div>
    );
  }

  if (sortKey === 'lastIssue' && lastIssue) {
    const date = getRelativeDate(
      lastIssue.releaseDate,
      shortDateFormat,
      showRelativeDates,
      {
        timeFormat,
        timeForToday: false
      }
    );

    return (
      <div className={styles.info}>
        {`Last Issue ${date}`}
      </div>
    );
  }

  if (sortKey === 'issueCount') {
    let issues = '1 issue';

    if (issueCount === 0) {
      issues = 'No issues';
    } else if (issueCount > 1) {
      issues = `${issueCount} issues`;
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
        {path}
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

SeriesIndexPosterInfo.propTypes = {
  qualityProfile: PropTypes.object.isRequired,
  showQualityProfile: PropTypes.bool.isRequired,
  added: PropTypes.string,
  nextIssue: PropTypes.object,
  lastIssue: PropTypes.object,
  issueCount: PropTypes.number.isRequired,
  path: PropTypes.string.isRequired,
  sizeOnDisk: PropTypes.number,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default SeriesIndexPosterInfo;
