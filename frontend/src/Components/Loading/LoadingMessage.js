import React from 'react';
import styles from './LoadingMessage.css';

const messages = [
  'Downloading more RAM',
  'Flipping to the next page...',
  'Previously on Panelarr...',
  'Bleep Bloop.',
  'Locating the required gigapixels to render...',
  'Spinning up the hamster wheel...',
  'At least you\'re not on hold',
  'Hum something loud while others stare',
  'Loading humorous message... Please Wait',
  'Checking the pull list...',
  'Bagging and boarding...',
  'Congratulations! You are the 1000th visitor.',
  'HELP! I\'m being held hostage and forced to write these stupid lines!',
  'Scanning for missing issues...',
  'I\'ll be here all week',
  'Reading the fine print in the speech bubbles...',
  'Organizing the long boxes...',
  'To be continued...'
];

let message = null;

function LoadingMessage() {
  if (!message) {
    const index = Math.floor(Math.random() * messages.length);
    message = messages[index];
  }

  return (
    <div className={styles.loadingMessage}>
      {message}
    </div>
  );
}

export default LoadingMessage;
