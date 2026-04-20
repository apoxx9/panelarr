import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingOption from './NamingOption';
import styles from './NamingModal.css';

const separatorOptions = [
  { key: ' ', value: 'Space ( )' },
  { key: '.', value: 'Period (.)' },
  { key: '_', value: 'Underscore (_)' },
  { key: '-', value: 'Dash (-)' }
];

const caseOptions = [
  { key: 'title', value: 'Default Case' },
  { key: 'lower', value: 'Lowercase' },
  { key: 'upper', value: 'Uppercase' }
];

const fileNameTokens = [
  {
    token: '{Series Name} - {Issue Title} - {Quality Full}',
    example: 'Series Name - Issue Title - CBZ Proper'
  },
  {
    token: '{Series.Name}.{Issue.Title}.{Quality.Full}',
    example: 'Series.Name.Issue.Title.CBZ'
  },
  {
    token: '{Series Name} - {Issue Title}{ (PartNumber)}',
    example: 'Series Name - Issue Title (2)'
  },
  {
    token: '{Series Name} - {Issue Title}{ (PartNumber/PartCount)}',
    example: 'Series Name - Issue Title (2/10)'
  }
];

const seriesTokens = [
  { token: '{Series Name}', example: 'Series\'s Name' },

  { token: '{Series NameThe}', example: 'Series\'s Name, The' },

  { token: '{Series NameFirstCharacter}', example: 'A' },

  { token: '{Series CleanName}', example: 'Series Name' },

  { token: '{Series SortName}', example: 'Name, Series' },

  { token: '{Series Disambiguation}', example: 'Disambiguation' }
];

const issueTokens = [
  { token: '{Issue Title}', example: 'The Issue\'s Title!: Subtitle!' },

  { token: '{Issue TitleThe}', example: 'Issue\'s Title!, The: Subtitle!' },

  { token: '{Issue CleanTitle}', example: 'The Issues Title!: Subtitle' },

  { token: '{Issue TitleNoSub}', example: 'The Issue\'s Title!' },

  { token: '{Issue TitleTheNoSub}', example: 'Issue\'s Title!, The' },

  { token: '{Issue CleanTitleNoSub}', example: 'The Issues Title!' },

  { token: '{Issue Subtitle}', example: 'Subtitle!' },

  { token: '{Issue SubtitleThe}', example: 'Subtitle!, The' },

  { token: '{Issue CleanSubtitle}', example: 'Subtitle' },

  { token: '{Issue Disambiguation}', example: 'Disambiguation' },

  { token: '{Issue Series}', example: 'Series Title' },

  { token: '{Issue SeriesPosition}', example: '1' },

  { token: '{Issue SeriesTitle}', example: 'Series Title #1' },

  { token: '{PartNumber:0}', example: '2' },
  { token: '{PartNumber:00}', example: '02' },
  { token: '{PartCount:0}', example: '9' },
  { token: '{PartCount:00}', example: '09' }
];

const releaseDateTokens = [
  { token: '{Release Year}', example: '2016' },
  { token: '{Release YearFirst}', example: '2015' }
];

const qualityTokens = [
  { token: '{Quality Full}', example: 'CBZ Proper' },
  { token: '{Quality Title}', example: 'CBZ' }
];

const otherTokens = [
  { token: '{Release Group}', example: 'Rls Grp' },
  { token: '{Custom Formats}', example: 'iNTERNAL' }
];

const originalTokens = [
  { token: '{Original Title}', example: 'Series.Name.Issue.Name.2018.CBZ-EVOLVE' },
  { token: '{Original Filename}', example: '01 - issue name' }
];

class NamingModal extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._selectionStart = null;
    this._selectionEnd = null;

    this.state = {
      separator: ' ',
      case: 'title'
    };
  }

  //
  // Listeners

  onTokenSeparatorChange = (event) => {
    this.setState({ separator: event.value });
  };

  onTokenCaseChange = (event) => {
    this.setState({ case: event.value });
  };

  onInputSelectionChange = (selectionStart, selectionEnd) => {
    this._selectionStart = selectionStart;
    this._selectionEnd = selectionEnd;
  };

  onOptionPress = ({ isFullFilename, tokenValue }) => {
    const {
      name,
      value,
      onInputChange
    } = this.props;

    const selectionStart = this._selectionStart;
    const selectionEnd = this._selectionEnd;

    if (isFullFilename) {
      onInputChange({ name, value: tokenValue });
    } else if (selectionStart == null) {
      onInputChange({
        name,
        value: `${value}${tokenValue}`
      });
    } else {
      const start = value.substring(0, selectionStart);
      const end = value.substring(selectionEnd);
      const newValue = `${start}${tokenValue}${end}`;

      onInputChange({ name, value: newValue });
      this._selectionStart = newValue.length - 1;
      this._selectionEnd = newValue.length - 1;
    }
  };

  //
  // Render

  render() {
    const {
      name,
      value,
      isOpen,
      advancedSettings,
      issue,
      additional,
      onInputChange,
      onModalClose
    } = this.props;

    const {
      separator: tokenSeparator,
      case: tokenCase
    } = this.state;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent onModalClose={onModalClose}>
          <ModalHeader>
            File Name Tokens
          </ModalHeader>

          <ModalBody>
            <div className={styles.namingSelectContainer}>
              <SelectInput
                className={styles.namingSelect}
                name="separator"
                value={tokenSeparator}
                values={separatorOptions}
                onChange={this.onTokenSeparatorChange}
              />

              <SelectInput
                className={styles.namingSelect}
                name="case"
                value={tokenCase}
                values={caseOptions}
                onChange={this.onTokenCaseChange}
              />
            </div>

            {
              !advancedSettings &&
                <FieldSet legend={translate('FileNames')}>
                  <div className={styles.groups}>
                    {
                      fileNameTokens.map(({ token, example }) => {
                        return (
                          <NamingOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            isFullFilename={true}
                            tokenSeparator={tokenSeparator}
                            tokenCase={tokenCase}
                            size={sizes.LARGE}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

            <FieldSet legend={translate('Series')}>
              <div className={styles.groups}>
                {
                  seriesTokens.map(({ token, example }) => {
                    return (
                      <NamingOption
                        key={token}
                        name={name}
                        value={value}
                        token={token}
                        example={example}
                        tokenSeparator={tokenSeparator}
                        tokenCase={tokenCase}
                        onPress={this.onOptionPress}
                      />
                    );
                  }
                  )
                }
              </div>
            </FieldSet>

            {
              issue &&
                <div>
                  <FieldSet legend={translate('Issue')}>
                    <div className={styles.groups}>
                      {
                        issueTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('ReleaseDate')}>
                    <div className={styles.groups}>
                      {
                        releaseDateTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
            }

            {
              additional &&
                <div>
                  <FieldSet legend={translate('Quality')}>
                    <div className={styles.groups}>
                      {
                        qualityTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Other')}>
                    <div className={styles.groups}>
                      {
                        otherTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>

                  <FieldSet legend={translate('Original')}>
                    <div className={styles.groups}>
                      {
                        originalTokens.map(({ token, example }) => {
                          return (
                            <NamingOption
                              key={token}
                              name={name}
                              value={value}
                              token={token}
                              example={example}
                              tokenSeparator={tokenSeparator}
                              tokenCase={tokenCase}
                              size={sizes.LARGE}
                              onPress={this.onOptionPress}
                            />
                          );
                        }
                        )
                      }
                    </div>
                  </FieldSet>
                </div>
            }
          </ModalBody>

          <ModalFooter>
            <TextInput
              name={name}
              value={value}
              onChange={onInputChange}
              onSelectionChange={this.onInputSelectionChange}
            />
            <Button onPress={onModalClose}>
              Close
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

NamingModal.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  isOpen: PropTypes.bool.isRequired,
  advancedSettings: PropTypes.bool.isRequired,
  issue: PropTypes.bool.isRequired,
  additional: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

NamingModal.defaultProps = {
  issue: false,
  additional: false
};

export default NamingModal;
