import PropTypes from 'prop-types';
import React from 'react';
import ProgressBar from 'Components/ProgressBar';
import { sizes } from 'Helpers/Props';
import getProgressBarKind from 'Utilities/Series/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './IssueIndexProgressBar.css';

function IssueIndexProgressBar(props) {
  const {
    monitored,
    issueCount,
    issueFileCount,
    totalIssueCount,
    posterWidth,
    detailedProgressBar
  } = props;

  const progress = issueFileCount && issueCount ? (totalIssueCount / issueCount) * 100 : 0;
  const text = `${issueFileCount ? issueCount : 0} / ${totalIssueCount}`;

  return (
    <ProgressBar
      className={styles.progressBar}
      containerClassName={styles.progress}
      progress={100}
      kind={getProgressBarKind('ended', monitored, progress)}
      size={detailedProgressBar ? sizes.MEDIUM : sizes.SMALL}
      showText={detailedProgressBar}
      text={text}
      title={translate('IssueProgressBarText', {
        issueCount: issueFileCount ? issueCount : 0,
        issueFileCount,
        totalIssueCount
      })}
      width={posterWidth}
    />
  );
}

IssueIndexProgressBar.propTypes = {
  monitored: PropTypes.bool.isRequired,
  issueCount: PropTypes.number.isRequired,
  issueFileCount: PropTypes.number.isRequired,
  totalIssueCount: PropTypes.number.isRequired,
  posterWidth: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired
};

export default IssueIndexProgressBar;
