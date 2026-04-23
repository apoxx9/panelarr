import PropTypes from 'prop-types';
import React, { Component } from 'react';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import styles from './SetupWizard.css';

class RootFolderStep extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      path: '',
      existingFolders: [],
      isFetching: false,
      isSaving: false,
      error: null
    };
  }

  componentDidMount() {
    this.fetchRootFolders();
  }

  //
  // Control

  fetchRootFolders() {
    this.setState({ isFetching: true });

    const { request } = createAjaxRequest({
      url: '/rootfolder',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({
        existingFolders: data || [],
        isFetching: false
      });

      this.props.onValidationChange(!!(data && data.length));
    });

    request.fail(() => {
      this.setState({
        isFetching: false,
        error: 'Failed to load root folders.'
      });
    });
  }

  //
  // Listeners

  onPathChange = (event) => {
    this.setState({ path: event.target.value });
  };

  onAddFolder = () => {
    const { path } = this.state;

    if (!path.trim()) {
      return;
    }

    this.setState({ isSaving: true, error: null });

    const { request } = createAjaxRequest({
      url: '/rootfolder',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify({
        name: path.trim().split('/').filter(Boolean).pop() || 'Comics',
        path: path.trim()
      })
    });

    request.done((data) => {
      const existingFolders = [...this.state.existingFolders, data];

      this.setState({
        path: '',
        existingFolders,
        isSaving: false
      });

      this.props.onValidationChange(true);
    });

    request.fail((xhr) => {
      let message = 'Failed to add root folder.';

      try {
        const response = JSON.parse(xhr.responseText);

        if (response && response.length && response[0].errorMessage) {
          message = response[0].errorMessage;
        }
      } catch (e) {
        // use default message
      }

      this.setState({
        isSaving: false,
        error: message
      });
    });
  };

  onKeyDown = (event) => {
    if (event.key === 'Enter') {
      this.onAddFolder();
    }
  };

  //
  // Render

  render() {
    const {
      path,
      existingFolders,
      isFetching,
      isSaving,
      error
    } = this.state;

    if (isFetching) {
      return (
        <div>
          <h2 className={styles.stepTitle}>Root Folder</h2>
          <p className={styles.stepDescription}>Loading...</p>
        </div>
      );
    }

    return (
      <div>
        <h2 className={styles.stepTitle}>Root Folder</h2>

        <p className={styles.stepDescription}>
          Add the folder where your comic collection is stored.
          Panelarr will scan this folder for existing comics.
        </p>

        <div className={styles.formContainer}>
          <div className={styles.formGroup}>
            <label className={styles.formLabel}>Path</label>

            <div style={{ display: 'flex', gap: 8 }}>
              <input
                className={styles.formInput}
                type="text"
                value={path}
                onChange={this.onPathChange}
                onKeyDown={this.onKeyDown}
                placeholder="/path/to/comics"
                style={{ flex: 1 }}
              />

              <button
                className={styles.buttonPrimary}
                onClick={this.onAddFolder}
                disabled={!path.trim() || isSaving}
                style={{ flexShrink: 0 }}
              >
                {isSaving ? 'Adding...' : 'Add'}
              </button>
            </div>
          </div>

          {
            error &&
              <div className={styles.error}>{error}</div>
          }

          {
            existingFolders.length > 0 &&
              <div className={styles.existingFolders}>
                <div className={styles.existingFoldersTitle}>
                  Configured Root Folders
                </div>

                {
                  existingFolders.map((folder) => {
                    return (
                      <div
                        key={folder.id}
                        className={styles.folderItem}
                      >
                        <span className={styles.folderIcon}>&bull;</span>
                        {folder.path}
                      </div>
                    );
                  })
                }
              </div>
          }
        </div>
      </div>
    );
  }
}

RootFolderStep.propTypes = {
  onValidationChange: PropTypes.func.isRequired
};

export default RootFolderStep;
