import PropTypes from 'prop-types';
import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Series/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './SeriesIndexProgressBar.css';

function SeriesIndexProgressBar(props) {
  const {
    monitored,
    status,
    issueCount,
    availableIssueCount,
    issueFileCount,
    totalIssueCount,
    posterWidth,
    detailedProgressBar
  } = props;

  const progress = issueCount ? (availableIssueCount / issueCount) * 100 : 100;
  const text = `${availableIssueCount} / ${issueCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={progress}
      kind={getProgressBarKind(status, monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('SeriesProgressBarText', { issueCount, availableIssueCount, issueFileCount, totalIssueCount })}
      width={posterWidth}
    />
  );
}

SeriesIndexProgressBar.propTypes = {
  monitored: PropTypes.bool.isRequired,
  status: PropTypes.string.isRequired,
  issueCount: PropTypes.number.isRequired,
  availableIssueCount: PropTypes.number.isRequired,
  issueFileCount: PropTypes.number.isRequired,
  totalIssueCount: PropTypes.number.isRequired,
  posterWidth: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired
};

export default SeriesIndexProgressBar;
