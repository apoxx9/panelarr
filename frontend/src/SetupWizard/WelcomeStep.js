import PropTypes from 'prop-types';
import React from 'react';
import styles from './SetupWizard.css';

function WelcomeStep() {
  return (
    <div className={styles.welcomeContainer}>
      <img
        className={styles.logo}
        src={`${window.Panelarr.urlBase}/Content/Images/logo.png`}
        alt="Panelarr"
      />

      <h1 className={styles.welcomeTitle}>
        Welcome to Panelarr
      </h1>

      <p className={styles.welcomeDescription}>
        Let's get you set up. This wizard will help you configure the
        essentials to start managing your comic collection.
      </p>
    </div>
  );
}

WelcomeStep.propTypes = {};

export default WelcomeStep;
