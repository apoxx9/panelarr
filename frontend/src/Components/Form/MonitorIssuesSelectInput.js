import PropTypes from 'prop-types';
import React from 'react';
import monitorOptions from 'Utilities/Series/monitorOptions';
import translate from 'Utilities/String/translate';
import SelectInput from './SelectInput';

function MonitorIssuesSelectInput(props) {
  const {
    includeNoChange,
    includeMixed,
    includeSpecificIssue,
    ...otherProps
  } = props;

  const values = [...monitorOptions];

  if (includeNoChange) {
    values.unshift({
      key: 'noChange',
      value: translate('NoChange'),
      isDisabled: true
    });
  }

  if (includeMixed) {
    values.unshift({
      key: 'mixed',
      value: '(Mixed)',
      isDisabled: true
    });
  }

  if (includeSpecificIssue) {
    values.push({
      key: 'specificIssue',
      value: 'Only This Issue'
    });
  }

  return (
    <SelectInput
      values={values}
      {...otherProps}
    />
  );
}

MonitorIssuesSelectInput.propTypes = {
  includeNoChange: PropTypes.bool.isRequired,
  includeMixed: PropTypes.bool.isRequired,
  includeSpecificIssue: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

MonitorIssuesSelectInput.defaultProps = {
  includeNoChange: false,
  includeMixed: false,
  includeSpecificIssue: false
};

export default MonitorIssuesSelectInput;
