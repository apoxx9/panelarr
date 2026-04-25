import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import translate from 'Utilities/String/translate';

class MetadataProvider extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isTesting: false,
      testResult: null,
      testError: null,
      isTestingComicVine: false,
      comicVineTestResult: null,
      comicVineTestError: null
    };
  }

  //
  // Listeners

  onTestMetronPress = () => {
    this.setState({
      isTesting: true,
      testResult: null,
      testError: null
    });

    const { request } = createAjaxRequest({
      url: '/config/metadataprovider/test',
      dataType: 'json'
    });

    request.done((data) => {
      if (data.isValid) {
        this.setState({
          isTesting: false,
          testResult: 'success',
          testError: null
        });
      } else {
        this.setState({
          isTesting: false,
          testResult: 'failure',
          testError: data.message || 'Unknown error'
        });
      }
    });

    request.fail((xhr) => {
      this.setState({
        isTesting: false,
        testResult: 'failure',
        testError: xhr.responseJSON ? xhr.responseJSON.message : 'Unable to reach the server'
      });
    });
  };

  onTestComicVinePress = () => {
    this.setState({
      isTestingComicVine: true,
      comicVineTestResult: null,
      comicVineTestError: null
    });

    const { request } = createAjaxRequest({
      url: '/config/metadataprovider/testcomicvine',
      dataType: 'json'
    });

    request.done((data) => {
      if (data.isValid) {
        this.setState({
          isTestingComicVine: false,
          comicVineTestResult: 'success',
          comicVineTestError: null
        });
      } else {
        this.setState({
          isTestingComicVine: false,
          comicVineTestResult: 'failure',
          comicVineTestError: data.message || 'Unknown error'
        });
      }
    });

    request.fail((xhr) => {
      this.setState({
        isTestingComicVine: false,
        comicVineTestResult: 'failure',
        comicVineTestError: xhr.responseJSON ? xhr.responseJSON.message : 'Unable to reach the server'
      });
    });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      error,
      settings,
      hasSettings,
      onInputChange
    } = this.props;

    const {
      isTesting,
      testResult,
      testError,
      isTestingComicVine,
      comicVineTestResult,
      comicVineTestError
    } = this.state;

    return (

      <div>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadMetadataProviderSettings')}
            </Alert>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FieldSet legend="Metron Credentials">
                <FormGroup>
                  <FormLabel>Username</FormLabel>
                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="metronUsername"
                    helpText="Your metron.cloud username (free registration)"
                    onChange={onInputChange}
                    {...settings.metronUsername}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>Password</FormLabel>
                  <FormInputGroup
                    type={inputTypes.PASSWORD}
                    name="metronPassword"
                    helpText="Your metron.cloud password"
                    onChange={onInputChange}
                    {...settings.metronPassword}
                  />
                </FormGroup>

                <div>
                  <SpinnerButton
                    kind={kinds.PRIMARY}
                    isSpinning={isTesting}
                    onPress={this.onTestMetronPress}
                  >
                    Test Metron
                  </SpinnerButton>

                  {
                    testResult === 'success' &&
                      <Alert
                        kind={kinds.SUCCESS}
                        className={undefined}
                      >
                        Metron credentials are valid!
                      </Alert>
                  }

                  {
                    testResult === 'failure' &&
                      <Alert
                        kind={kinds.DANGER}
                        className={undefined}
                      >
                        {testError}
                      </Alert>
                  }
                </div>
              </FieldSet>

              <FieldSet legend="ComicVine (Fallback)">
                <FormGroup>
                  <FormLabel>ComicVine API Key</FormLabel>
                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="comicVineApiKey"
                    helpText="Optional fallback metadata source (get key from comicvine.gamespot.com)"
                    onChange={onInputChange}
                    {...settings.comicVineApiKey}
                  />
                </FormGroup>

                <div>
                  <SpinnerButton
                    kind={kinds.PRIMARY}
                    isSpinning={isTestingComicVine}
                    onPress={this.onTestComicVinePress}
                  >
                    Test ComicVine
                  </SpinnerButton>

                  {
                    comicVineTestResult === 'success' &&
                      <Alert
                        kind={kinds.SUCCESS}
                        className={undefined}
                      >
                        ComicVine API key is valid!
                      </Alert>
                  }

                  {
                    comicVineTestResult === 'failure' &&
                      <Alert
                        kind={kinds.DANGER}
                        className={undefined}
                      >
                        {comicVineTestError}
                      </Alert>
                  }
                </div>
              </FieldSet>

            </Form>
        }
      </div>

    );
  }
}

MetadataProvider.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default MetadataProvider;
