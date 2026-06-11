import PropTypes from 'prop-types';
import React, { Component } from 'react';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import CompleteStep from './CompleteStep';
import DownloadClientStep from './DownloadClientStep';
import IndexerStep from './IndexerStep';
import MetadataProviderStep from './MetadataProviderStep';
import RootFolderStep from './RootFolderStep';
import WelcomeStep from './WelcomeStep';
import styles from './SetupWizard.css';

const STEP_COUNT = 6;
const STEP_WELCOME = 0;
const STEP_METADATA = 1;
const STEP_ROOT_FOLDER = 2;
const STEP_INDEXER = 3;
const STEP_DOWNLOAD_CLIENT = 4;
const STEP_COMPLETE = 5;

class SetupWizard extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      currentStep: STEP_WELCOME,
      isCompleting: false,
      // Track which steps are valid (can proceed with Next)
      stepValidation: {
        [STEP_METADATA]: false,
        [STEP_ROOT_FOLDER]: false
      },
      // Which configurable steps actually have something configured
      // (shown in the completion summary)
      completedSteps: [false, false, false, false]
    };

    this._saveMetadata = null;
  }

  //
  // Listeners

  onNext = () => {
    const { currentStep } = this.state;

    // The metadata step's settings live in its own form — persist them when
    // advancing, and only advance (and mark configured) if the save succeeds.
    if (currentStep === STEP_METADATA && this._saveMetadata) {
      this._saveMetadata(() => {
        this.markCompleted(0);
        this.goToStep(currentStep + 1);
      });

      return;
    }

    if (currentStep === STEP_ROOT_FOLDER) {
      this.markCompleted(1);
    }

    this.goToStep(currentStep + 1);
  };

  onBack = () => {
    this.goToStep(this.state.currentStep - 1);
  };

  onSkip = () => {
    this.goToStep(this.state.currentStep + 1);
  };

  onRegisterMetadataSave = (saveFn) => {
    this._saveMetadata = saveFn;
  };

  onValidationChange = (stepIndex, isValid) => {
    this.setState((prevState) => ({
      stepValidation: {
        ...prevState.stepValidation,
        [stepIndex]: isValid
      }
    }));
  };

  onMetadataValidationChange = (isValid) => {
    this.onValidationChange(STEP_METADATA, isValid);
  };

  onRootFolderValidationChange = (isValid) => {
    this.onValidationChange(STEP_ROOT_FOLDER, isValid);
  };

  onIndexerConfiguredChange = (isConfigured) => {
    this.markCompleted(2, isConfigured);
  };

  onDownloadClientConfiguredChange = (isConfigured) => {
    this.markCompleted(3, isConfigured);
  };

  onComplete = () => {
    this.setState({ isCompleting: true });

    const { request } = createAjaxRequest({
      url: '/system/setup/complete',
      method: 'POST',
      dataType: 'json'
    });

    request.done(() => {
      window.location.href = getPathWithUrlBase('/');
    });

    request.fail(() => {
      // Even if the call fails, redirect so user isn't stuck
      window.location.href = getPathWithUrlBase('/');
    });
  };

  //
  // Control

  goToStep(step) {
    this.setState({
      currentStep: Math.min(Math.max(step, 0), STEP_COUNT - 1)
    });
  }

  markCompleted(summaryIndex, isCompleted = true) {
    this.setState((prevState) => {
      const completedSteps = [...prevState.completedSteps];
      completedSteps[summaryIndex] = isCompleted;

      return { completedSteps };
    });
  }

  //
  // Render

  renderProgressBar() {
    const { currentStep } = this.state;
    const steps = [];

    for (let i = 0; i < STEP_COUNT; i++) {
      if (i > 0) {
        steps.push(
          <div
            key={`line-${i}`}
            className={i <= currentStep ? styles.progressLineComplete : styles.progressLine}
          />
        );
      }

      let stepClass = styles.progressStep;

      if (i < currentStep) {
        stepClass = styles.progressStepComplete;
      } else if (i === currentStep) {
        stepClass = styles.progressStepActive;
      }

      steps.push(
        <div
          key={`step-${i}`}
          className={stepClass}
        />
      );
    }

    return (
      <div className={styles.progressBar}>
        {steps}
      </div>
    );
  }

  renderStep() {
    const { currentStep, completedSteps, isCompleting } = this.state;

    switch (currentStep) {
      case STEP_WELCOME:
        return <WelcomeStep />;

      case STEP_METADATA:
        return (
          <MetadataProviderStep
            onValidationChange={this.onMetadataValidationChange}
            onRegisterSave={this.onRegisterMetadataSave}
          />
        );

      case STEP_ROOT_FOLDER:
        return (
          <RootFolderStep
            onValidationChange={this.onRootFolderValidationChange}
          />
        );

      case STEP_INDEXER:
        return (
          <IndexerStep
            onConfiguredChange={this.onIndexerConfiguredChange}
          />
        );

      case STEP_DOWNLOAD_CLIENT:
        return (
          <DownloadClientStep
            onConfiguredChange={this.onDownloadClientConfiguredChange}
          />
        );

      case STEP_COMPLETE:
        return (
          <CompleteStep
            completedSteps={completedSteps}
            isCompleting={isCompleting}
            onComplete={this.onComplete}
          />
        );

      default:
        return null;
    }
  }

  renderButtons() {
    const { currentStep, stepValidation, completedSteps } = this.state;

    // No buttons on complete step (has its own button)
    if (currentStep === STEP_COMPLETE) {
      return null;
    }

    const showBack = currentStep > STEP_WELCOME;

    // Optional steps offer Skip until something has been configured
    const showSkip =
      (currentStep === STEP_INDEXER && !completedSteps[2]) ||
      (currentStep === STEP_DOWNLOAD_CLIENT && !completedSteps[3]);

    let canNext = true;

    if (currentStep === STEP_METADATA) {
      canNext = stepValidation[STEP_METADATA];
    } else if (currentStep === STEP_ROOT_FOLDER) {
      canNext = stepValidation[STEP_ROOT_FOLDER];
    }

    return (
      <div className={styles.buttons}>
        <div>
          {
            showBack &&
              <button
                className={styles.buttonSecondary}
                onClick={this.onBack}
              >
                Back
              </button>
          }
        </div>

        <div className={styles.buttonsRight}>
          {
            showSkip &&
              <button
                className={styles.buttonSecondary}
                onClick={this.onSkip}
              >
                Skip
              </button>
          }

          {
            !showSkip &&
              <button
                className={styles.buttonPrimary}
                onClick={this.onNext}
                disabled={!canNext}
              >
                Next
              </button>
          }
        </div>
      </div>
    );
  }

  render() {
    return (
      <div className={styles.wizard}>
        <div className={styles.card}>
          {this.renderProgressBar()}

          <div className={styles.stepContent}>
            {this.renderStep()}
          </div>

          {this.renderButtons()}
        </div>
      </div>
    );
  }
}

SetupWizard.propTypes = {
  systemStatus: PropTypes.object.isRequired
};

export default SetupWizard;
