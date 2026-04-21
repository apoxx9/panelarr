import PropTypes from 'prop-types';
import React from 'react';
import styles from './SetupWizard.css';

const STEP_LABELS = [
  'Metadata Provider',
  'Root Folder',
  'Indexer',
  'Download Client'
];

function CompleteStep(props) {
  const {
    completedSteps,
    isCompleting,
    onComplete
  } = props;

  return (
    <div className={styles.completeContainer}>
      <h2 className={styles.completeTitle}>
        Setup Complete!
      </h2>

      <p className={styles.completeDescription}>
        Here's a summary of what was configured. You can always change
        these settings later.
      </p>

      <ul className={styles.summaryList}>
        {
          STEP_LABELS.map((label, index) => {
            const isComplete = completedSteps[index];

            return (
              <li
                key={label}
                className={styles.summaryItem}
              >
                <span className={isComplete ? styles.summaryIconComplete : styles.summaryIconSkipped}>
                  {isComplete ? '\u2713' : '\u2014'}
                </span>
                {label}
                {!isComplete && <span style={{ color: '#666', marginLeft: 'auto', fontSize: 12 }}>Skipped</span>}
              </li>
            );
          })
        }
      </ul>

      <button
        className={styles.startButton}
        onClick={onComplete}
        disabled={isCompleting}
      >
        {isCompleting ? 'Starting...' : 'Start Using Panelarr'}
      </button>
    </div>
  );
}

CompleteStep.propTypes = {
  completedSteps: PropTypes.arrayOf(PropTypes.bool).isRequired,
  isCompleting: PropTypes.bool.isRequired,
  onComplete: PropTypes.func.isRequired
};

export default CompleteStep;
