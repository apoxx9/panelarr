import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import NamingModal from './NamingModal';
import styles from './Naming.css';

class Naming extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isNamingModalOpen: false,
      namingModalOptions: null
    };
  }

  //
  // Listeners

  onStandardNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'standardIssueFormat',
        issue: true,
        additional: true
      }
    });
  };

  onSeriesFolderNamingModalOpenClick = () => {
    this.setState({
      isNamingModalOpen: true,
      namingModalOptions: {
        name: 'seriesFolderFormat'
      }
    });
  };

  onNamingModalClose = () => {
    this.setState({ isNamingModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      settings,
      hasSettings,
      examples,
      examplesPopulated,
      onInputChange
    } = this.props;

    const {
      isNamingModalOpen,
      namingModalOptions
    } = this.state;

    const renameComics = hasSettings && settings.renameComics.value;
    const replaceIllegalCharacters = hasSettings && settings.replaceIllegalCharacters.value;

    const colonReplacementOptions = [
      { key: 0, value: translate('Delete') },
      { key: 1, value: translate('ReplaceWithDash') },
      { key: 2, value: translate('ReplaceWithSpaceDash') },
      { key: 3, value: translate('ReplaceWithSpaceDashSpace') },
      { key: 4, value: translate('SmartReplace'), hint: translate('DashOrSpaceDashDependingOnName') }
    ];

    const standardIssueFormatHelpTexts = [];
    const standardIssueFormatErrors = [];
    const seriesFolderFormatHelpTexts = [];
    const seriesFolderFormatErrors = [];

    if (examplesPopulated) {
      if (examples.singleIssueExample) {
        standardIssueFormatHelpTexts.push(`Single Issue: ${examples.singleIssueExample}`);
      } else {
        standardIssueFormatErrors.push({ message: 'Single Issue: Invalid Format' });
      }

      if (examples.multiPartIssueExample) {
        standardIssueFormatHelpTexts.push(`Multi-part Issue: ${examples.multiPartIssueExample}`);
      } else {
        standardIssueFormatErrors.push({ message: 'Multi-part Issue: Invalid Format' });
      }

      if (examples.seriesFolderExample) {
        seriesFolderFormatHelpTexts.push(`Example: ${examples.seriesFolderExample}`);
      } else {
        seriesFolderFormatErrors.push({ message: 'Invalid Format' });
      }
    }

    return (
      <FieldSet legend={translate('IssueNaming')}>
        {
          isFetching &&
            <LoadingIndicator />
        }

        {
          !isFetching && error &&
            <Alert kind={kinds.DANGER}>
              {translate('UnableToLoadNamingSettings')}
            </Alert>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('RenameIssues')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="renameComics"
                  helpText={translate('RenameIssuesHelpText')}
                  onChange={onInputChange}
                  {...settings.renameComics}
                />
              </FormGroup>

              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>
                  {translate('ReplaceIllegalCharacters')}
                </FormLabel>

                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="replaceIllegalCharacters"
                  helpText={translate('ReplaceIllegalCharactersHelpText')}
                  onChange={onInputChange}
                  {...settings.replaceIllegalCharacters}
                />
              </FormGroup>

              {
                replaceIllegalCharacters ?
                  <FormGroup>
                    <FormLabel>
                      {translate('ColonReplacement')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="colonReplacementFormat"
                      values={colonReplacementOptions}
                      onChange={onInputChange}
                      {...settings.colonReplacementFormat}
                    />
                  </FormGroup> :
                  null
              }

              {
                renameComics &&
                  <div>
                    <FormGroup size={sizes.LARGE}>
                      <FormLabel>
                        {translate('StandardIssueFormat')}
                      </FormLabel>

                      <FormInputGroup
                        inputClassName={styles.namingInput}
                        type={inputTypes.TEXT}
                        name="standardIssueFormat"
                        buttons={<FormInputButton onPress={this.onStandardNamingModalOpenClick}>?</FormInputButton>}
                        onChange={onInputChange}
                        {...settings.standardIssueFormat}
                        helpTexts={standardIssueFormatHelpTexts}
                        errors={[...standardIssueFormatErrors, ...settings.standardIssueFormat.errors]}
                      />
                    </FormGroup>
                  </div>
              }

              <FormGroup
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {translate('SeriesFolderFormat')}
                </FormLabel>

                <FormInputGroup
                  inputClassName={styles.namingInput}
                  type={inputTypes.TEXT}
                  name="seriesFolderFormat"
                  buttons={<FormInputButton onPress={this.onSeriesFolderNamingModalOpenClick}>?</FormInputButton>}
                  onChange={onInputChange}
                  {...settings.seriesFolderFormat}
                  helpTexts={['Used when adding a new series or moving a series via the series editor', ...seriesFolderFormatHelpTexts]}
                  errors={[...seriesFolderFormatErrors, ...settings.seriesFolderFormat.errors]}
                />
              </FormGroup>

              {
                namingModalOptions &&
                  <NamingModal
                    isOpen={isNamingModalOpen}
                    advancedSettings={advancedSettings}
                    {...namingModalOptions}
                    value={settings[namingModalOptions.name].value}
                    onInputChange={onInputChange}
                    onModalClose={this.onNamingModalClose}
                  />
              }
            </Form>
        }
      </FieldSet>
    );
  }

}

Naming.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  examples: PropTypes.object.isRequired,
  examplesPopulated: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default Naming;
