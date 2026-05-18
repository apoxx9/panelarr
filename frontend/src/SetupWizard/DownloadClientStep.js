import PropTypes from 'prop-types';
import React, { Component } from 'react';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import styles from './SetupWizard.css';

function getInputType(fieldType) {
  switch (fieldType) {
    case 'password':
      return 'password';
    case 'number':
      return 'number';
    case 'checkbox':
      return 'checkbox';
    default:
      return 'text';
  }
}

class DownloadClientStep extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      schemas: [],
      existingClients: [],
      selectedSchemaIndex: -1,
      fields: [],
      name: '',
      isFetchingSchema: false,
      isSaving: false,
      isTesting: false,
      testStatus: null,
      testMessage: '',
      saveError: null
    };
  }

  componentDidMount() {
    this.fetchSchema();
    this.fetchExisting();
  }

  //
  // Control

  fetchSchema() {
    this.setState({ isFetchingSchema: true });

    const { request } = createAjaxRequest({
      url: '/downloadclient/schema',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({
        schemas: data || [],
        isFetchingSchema: false
      });
    });

    request.fail(() => {
      this.setState({
        isFetchingSchema: false,
        saveError: 'Failed to load download client schemas.'
      });
    });
  }

  fetchExisting() {
    const { request } = createAjaxRequest({
      url: '/downloadclient',
      dataType: 'json'
    });

    request.done((data) => {
      this.setState({ existingClients: data || [] });
    });
  }

  getPayload() {
    const { schemas, selectedSchemaIndex, fields, name } = this.state;
    const schema = schemas[selectedSchemaIndex];

    return {
      enable: true,
      name: name || schema.implementationName,
      implementation: schema.implementation,
      implementationName: schema.implementationName,
      configContract: schema.configContract,
      protocol: schema.protocol || 'unknown',
      priority: 1,
      removeCompletedDownloads: true,
      removeFailedDownloads: true,
      fields: fields.map((field) => ({
        name: field.name,
        value: field.value
      }))
    };
  }

  //
  // Listeners

  onSchemaChange = (event) => {
    const index = parseInt(event.target.value);

    if (index < 0) {
      this.setState({
        selectedSchemaIndex: -1,
        fields: [],
        name: ''
      });
      return;
    }

    const schema = this.state.schemas[index];
    const fields = (schema.fields || []).map((field) => ({
      ...field,
      value: field.value != null ? field.value : ''
    }));

    this.setState({
      selectedSchemaIndex: index,
      fields,
      name: schema.implementationName || ''
    });
  };

  onNameChange = (event) => {
    this.setState({ name: event.target.value });
  };

  onFieldChange = (event, fieldName) => {
    const { fields } = this.state;
    const target = event.target;
    const value = target.type === 'checkbox' ? target.checked : target.value;

    const updatedFields = fields.map((field) => {
      if (field.name === fieldName) {
        return { ...field, value };
      }

      return field;
    });

    this.setState({ fields: updatedFields });
  };

  onTest = () => {
    this.setState({
      isTesting: true,
      testStatus: null,
      testMessage: ''
    });

    const { request } = createAjaxRequest({
      url: '/downloadclient/test',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify(this.getPayload())
    });

    request.done(() => {
      this.setState({
        isTesting: false,
        testStatus: 'success',
        testMessage: 'Connection successful!'
      });
    });

    request.fail((xhr) => {
      let message = 'Test failed.';

      try {
        const response = JSON.parse(xhr.responseText);

        if (response && response.length && response[0].errorMessage) {
          message = response[0].errorMessage;
        } else if (response && response.message) {
          message = response.message;
        }
      } catch (e) {
        // use default message
      }

      this.setState({
        isTesting: false,
        testStatus: 'failure',
        testMessage: message
      });
    });
  };

  onSave = () => {
    this.setState({ isSaving: true, saveError: null });

    const { request } = createAjaxRequest({
      url: '/downloadclient',
      method: 'POST',
      dataType: 'json',
      data: JSON.stringify(this.getPayload())
    });

    request.done(() => {
      this.setState({
        isSaving: false,
        selectedSchemaIndex: -1,
        fields: [],
        name: '',
        testStatus: null,
        testMessage: '',
        saveError: null
      });
      this.fetchExisting();
    });

    request.fail((xhr) => {
      let message = 'Failed to save download client.';

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
        saveError: message
      });
    });
  };

  //
  // Render

  renderField(field) {
    const inputType = getInputType(field.type);

    if (inputType === 'checkbox') {
      return (
        <div key={field.name} className={styles.formGroup}>
          <label className={styles.formLabel}>
            <input
              type="checkbox"
              checked={!!field.value}
              onChange={(e) => this.onFieldChange(e, field.name)}
              style={{ marginRight: 8 }}
            />
            {field.label}
          </label>

          {
            field.helpText &&
              <div className={styles.helpText}>{field.helpText}</div>
          }
        </div>
      );
    }

    return (
      <div key={field.name} className={styles.formGroup}>
        <label className={styles.formLabel}>{field.label}</label>

        <input
          className={styles.formInput}
          type={inputType}
          value={field.value || ''}
          onChange={(e) => this.onFieldChange(e, field.name)}
          placeholder={field.helpText || ''}
        />

        {
          field.helpText &&
            <div className={styles.helpText}>{field.helpText}</div>
        }
      </div>
    );
  }

  render() {
    const {
      schemas,
      existingClients,
      selectedSchemaIndex,
      fields,
      name,
      isFetchingSchema,
      isSaving,
      isTesting,
      testStatus,
      testMessage,
      saveError
    } = this.state;

    if (isFetchingSchema) {
      return (
        <div>
          <h2 className={styles.stepTitle}>Download Client</h2>
          <p className={styles.stepDescription}>Loading...</p>
        </div>
      );
    }

    const hasSelection = selectedSchemaIndex >= 0;

    return (
      <div>
        <h2 className={styles.stepTitle}>Download Client</h2>

        <p className={styles.stepDescription}>
          Add a download client to download comics. You can skip this step and
          configure download clients later in Settings.
        </p>

        <div className={styles.formContainer}>
          {
            existingClients.length > 0 &&
              <div className={styles.existingFolders}>
                <div className={styles.existingFoldersTitle}>
                  Configured Download Clients
                </div>

                {
                  existingClients.map((client) => {
                    return (
                      <div
                        key={client.id}
                        className={styles.folderItem}
                      >
                        <span className={styles.folderIcon}>&bull;</span>
                        {client.name} ({client.implementationName})
                      </div>
                    );
                  })
                }
              </div>
          }

          <div className={styles.schemaSelector}>
            <div className={styles.formGroup}>
              <label className={styles.formLabel}>Client Type</label>

              <select
                className={styles.formSelect}
                value={selectedSchemaIndex}
                onChange={this.onSchemaChange}
              >
                <option value={-1}>Select a download client...</option>

                {
                  schemas.map((schema, index) => {
                    return (
                      <option
                        key={schema.implementation}
                        value={index}
                      >
                        {schema.implementationName}
                      </option>
                    );
                  })
                }
              </select>
            </div>
          </div>

          {
            hasSelection &&
              <div className={styles.schemaFields}>
                <div className={styles.formGroup}>
                  <label className={styles.formLabel}>Name</label>

                  <input
                    className={styles.formInput}
                    type="text"
                    value={name}
                    onChange={this.onNameChange}
                    placeholder="Client name"
                  />
                </div>

                {
                  fields.map((field) => {
                    if (field.hidden === 'hidden') {
                      return null;
                    }

                    return this.renderField(field);
                  })
                }

                <div className={styles.testButtonContainer}>
                  <button
                    className={styles.testButton}
                    onClick={this.onTest}
                    disabled={isTesting}
                  >
                    {isTesting ? 'Testing...' : 'Test'}
                  </button>

                  {
                    testStatus === 'success' &&
                      <span className={styles.testSuccess}>{testMessage}</span>
                  }

                  {
                    testStatus === 'failure' &&
                      <span className={styles.testFailure}>{testMessage}</span>
                  }
                </div>

                <div style={{ marginTop: 16 }}>
                  <button
                    className={styles.buttonPrimary}
                    onClick={this.onSave}
                    disabled={isSaving || !name}
                  >
                    {isSaving ? 'Saving...' : 'Save Download Client'}
                  </button>
                </div>
              </div>
          }

          {
            saveError &&
              <div className={styles.error}>{saveError}</div>
          }

          <div style={{ marginTop: 24, textAlign: 'right' }}>
            <button
              className={styles.buttonPrimary}
              onClick={this.props.onStepComplete}
            >
              {existingClients.length > 0 ? 'Next' : 'Skip'}
            </button>
          </div>
        </div>
      </div>
    );
  }
}

DownloadClientStep.propTypes = {
  onStepComplete: PropTypes.func.isRequired
};

export default DownloadClientStep;
