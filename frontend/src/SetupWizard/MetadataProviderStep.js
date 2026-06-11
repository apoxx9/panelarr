import PropTypes from 'prop-types';
import React, { Component } from 'react';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import styles from './SetupWizard.css';

class MetadataProviderStep extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      configId: null,
      metronUsername: '',
      metronPassword: '',
      comicVineApiKey: '',
      isFetching: false,
      isSaving: false,
      metronTestStatus: null,
      comicVineTestStatus: null,
      metronTestMessage: '',
      comicVineTestMessage: '',
      error: null
    };
  }

  componentDidMount() {
    this.fetchConfig();

    // Let the wizard save this step's settings when the user clicks Next —
    // otherwise credentials typed here would be silently discarded.
    this.props.onRegisterSave(this.saveConfig);
  }

  //
  // Control

  fetchConfig() {
    this.setState({ isFetching: true });

    const { request } = createAjaxRequest({
      url: '/config/metadataprovider',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({
        configId: data.id,
        metronUsername: data.metronUsername || '',
        metronPassword: data.metronPassword || '',
        comicVineApiKey: data.comicVineApiKey || '',
        isFetching: false
      });

      this.updateValidation(
        data.metronUsername || '',
        data.metronPassword || '',
        data.comicVineApiKey || ''
      );
    });

    request.fail(() => {
      this.setState({
        isFetching: false,
        error: 'Failed to load metadata provider settings.'
      });
    });
  }

  updateValidation(username, password, apiKey) {
    const hasMetron = !!(username && password);
    const hasComicVine = !!apiKey;

    this.props.onValidationChange(hasMetron || hasComicVine);
  }

  saveConfig = (callback) => {
    this.setState({ isSaving: true, error: null });

    const { request } = createAjaxRequest({
      url: '/config/metadataprovider',
      method: 'PUT',
      dataType: 'json',
      data: JSON.stringify({
        id: this.state.configId,
        metronUsername: this.state.metronUsername,
        metronPassword: this.state.metronPassword,
        comicVineApiKey: this.state.comicVineApiKey
      })
    });

    request.done(() => {
      this.setState({ isSaving: false });

      if (callback) {
        callback();
      }
    });

    request.fail(() => {
      this.setState({
        isSaving: false,
        error: 'Failed to save metadata provider settings.'
      });
    });
  };

  //
  // Listeners

  onInputChange = (event) => {
    const { name, value } = event.target;

    this.setState({ [name]: value }, () => {
      this.updateValidation(
        this.state.metronUsername,
        this.state.metronPassword,
        this.state.comicVineApiKey
      );
    });
  };

  onTestMetron = () => {
    this.setState({
      metronTestStatus: 'testing',
      metronTestMessage: ''
    });

    this.saveConfig(() => {
      const { request } = createAjaxRequest({
        url: '/config/metadataprovider/test',
        dataType: 'json'
      });

      request.done(() => {
        this.setState({
          metronTestStatus: 'success',
          metronTestMessage: 'Connection successful!'
        });
      });

      request.fail((xhr) => {
        let message = 'Connection failed.';

        try {
          const response = JSON.parse(xhr.responseText);

          if (response && response.message) {
            message = response.message;
          }
        } catch (e) {
          // use default message
        }

        this.setState({
          metronTestStatus: 'failure',
          metronTestMessage: message
        });
      });
    });
  };

  onTestComicVine = () => {
    this.setState({
      comicVineTestStatus: 'testing',
      comicVineTestMessage: ''
    });

    this.saveConfig(() => {
      const { request } = createAjaxRequest({
        url: '/config/metadataprovider/testcomicvine',
        dataType: 'json'
      });

      request.done(() => {
        this.setState({
          comicVineTestStatus: 'success',
          comicVineTestMessage: 'Connection successful!'
        });
      });

      request.fail((xhr) => {
        let message = 'Connection failed.';

        try {
          const response = JSON.parse(xhr.responseText);

          if (response && response.message) {
            message = response.message;
          }
        } catch (e) {
          // use default message
        }

        this.setState({
          comicVineTestStatus: 'failure',
          comicVineTestMessage: message
        });
      });
    });
  };

  //
  // Render

  renderTestStatus(status, message) {
    if (!status) {
      return null;
    }

    if (status === 'testing') {
      return (
        <span className={styles.testSpinner}>Testing...</span>
      );
    }

    if (status === 'success') {
      return (
        <span className={styles.testSuccess}>{message}</span>
      );
    }

    return (
      <span className={styles.testFailure}>{message}</span>
    );
  }

  render() {
    const {
      metronUsername,
      metronPassword,
      comicVineApiKey,
      isFetching,
      metronTestStatus,
      comicVineTestStatus,
      metronTestMessage,
      comicVineTestMessage,
      error
    } = this.state;

    if (isFetching) {
      return (
        <div>
          <h2 className={styles.stepTitle}>Metadata Provider</h2>
          <p className={styles.stepDescription}>Loading...</p>
        </div>
      );
    }

    const canTestMetron = !!(metronUsername && metronPassword);
    const canTestComicVine = !!comicVineApiKey;

    return (
      <div>
        <h2 className={styles.stepTitle}>Metadata Provider</h2>

        <p className={styles.stepDescription}>
          Configure at least one metadata source. Metron is recommended for
          the best comic metadata. ComicVine is optional.
        </p>

        <div className={styles.formContainer}>
          <div className={styles.formGroup}>
            <label className={styles.formLabel}>Metron Username</label>

            <input
              className={styles.formInput}
              type="text"
              name="metronUsername"
              value={metronUsername}
              onChange={this.onInputChange}
              placeholder="Your Metron username"
            />
          </div>

          <div className={styles.formGroup}>
            <label className={styles.formLabel}>Metron Password</label>

            <input
              className={styles.formInput}
              type="password"
              name="metronPassword"
              value={metronPassword}
              onChange={this.onInputChange}
              placeholder="Your Metron password"
            />
          </div>

          <div className={styles.testButtonContainer}>
            <button
              className={styles.testButton}
              onClick={this.onTestMetron}
              disabled={!canTestMetron || metronTestStatus === 'testing'}
            >
              Test Metron
            </button>

            {this.renderTestStatus(metronTestStatus, metronTestMessage)}
          </div>

          <div className={styles.formGroup} style={{ marginTop: 24 }}>
            <label className={styles.formLabel}>
              ComicVine API Key (optional)
            </label>

            <input
              className={styles.formInput}
              type="password"
              name="comicVineApiKey"
              value={comicVineApiKey}
              onChange={this.onInputChange}
              placeholder="Your ComicVine API key"
            />

            <div className={styles.helpText}>
              Optional. Get a key from comicvine.gamespot.com
            </div>
          </div>

          <div className={styles.testButtonContainer}>
            <button
              className={styles.testButton}
              onClick={this.onTestComicVine}
              disabled={!canTestComicVine || comicVineTestStatus === 'testing'}
            >
              Test ComicVine
            </button>

            {this.renderTestStatus(comicVineTestStatus, comicVineTestMessage)}
          </div>

          {
            error &&
              <div className={styles.error}>{error}</div>
          }
        </div>
      </div>
    );
  }
}

MetadataProviderStep.propTypes = {
  onValidationChange: PropTypes.func.isRequired,
  onRegisterSave: PropTypes.func.isRequired
};

export default MetadataProviderStep;
