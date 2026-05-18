import PropTypes from 'prop-types';
import React, { Component } from 'react';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import WelcomeStep from './WelcomeStep';
import MetadataProviderStep from './MetadataProviderStep';
import RootFolderStep from './RootFolderStep';
import IndexerStep from './IndexerStep';
import DownloadClientStep from './DownloadClientStep';
import CompleteStep from './CompleteStep';
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
      // Track which configurable steps were completed (for summary)
      completedSteps: [false, false, false, false]
    };
  }

  //
  // Listeners

  onNext = () => {
    const { currentStep, completedSteps } = this.state;

    // Mark current step as completed when moving forward via Next
    if (currentStep === STEP_METADATA) {
      completedSteps[0] = true;
    } else if (currentStep === STEP_ROOT_FOLDER) {
      completedSteps[1] = true;
    }

    this.setState({
      currentStep: Math.min(currentStep + 1, STEP_COUNT - 1),
      completedSteps: [...completedSteps]
    });
  };

  onBack = () => {
    this.setState({
      currentStep: Math.max(this.state.currentStep - 1, 0)
    });
  };

  onSkip = () => {
    this.setState({
      currentStep: Math.min(this.state.currentStep + 1, STEP_COUNT - 1)
    });
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

  onIndexerStepComplete = () => {
    const completedSteps = [...this.state.completedSteps];
    completedSteps[2] = true;

    this.setState({
      currentStep: STEP_DOWNLOAD_CLIENT,
      completedSteps
    });
  };

  onDownloadClientStepComplete = () => {
    const completedSteps = [...this.state.completedSteps];
    completedSteps[3] = true;

    this.setState({
      currentStep: STEP_COMPLETE,
      completedSteps
    });
  };

  onComplete = () => {
    this.setState({ isCompleting: true });

    const { request } = createAjaxRequest({
      url: '/system/setup/complete',
      method: 'POST',
      dataType: 'json'
    });

    request.done(() => {
      window.location.href = getPathWithUrlBase('/login');
    });

    request.fail(() => {
      // Even if the call fails, redirect so user isn't stuck
      window.location.href = getPathWithUrlBase('/login');
    });
  };

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
            onStepComplete={this.onIndexerStepComplete}
          />
        );

      case STEP_DOWNLOAD_CLIENT:
        return (
          <DownloadClientStep
            onStepComplete={this.onDownloadClientStepComplete}
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
    const { currentStep, stepValidation } = this.state;

    // No buttons on welcome (just Next) or complete step (has its own button)
    if (currentStep === STEP_COMPLETE) {
      return null;
    }

    const showBack = currentStep > STEP_WELCOME;
    const showSkip = currentStep === STEP_INDEXER || currentStep === STEP_DOWNLOAD_CLIENT;

    let canNext = true;

    if (currentStep === STEP_METADATA) {
      canNext = stepValidation[STEP_METADATA];
    } else if (currentStep === STEP_ROOT_FOLDER) {
      canNext = stepValidation[STEP_ROOT_FOLDER];
    }

    // Determine the correct Next action for the current step
    let onNextClick = this.onNext;

    if (currentStep === STEP_INDEXER) {
      onNextClick = this.onIndexerStepComplete;
    } else if (currentStep === STEP_DOWNLOAD_CLIENT) {
      onNextClick = this.onDownloadClientStepComplete;
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
          <button
            className={styles.buttonPrimary}
            onClick={onNextClick}
            disabled={!canNext}
          >
            Next
          </button>
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
