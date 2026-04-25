import PropTypes from 'prop-types';
import React from 'react';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';

function QualityDefinitionLimits(props) {
  const {
    bytes,
    message
  } = props;

  if (!bytes) {
    return <div>{message}</div>;
  }

  const fiftyPages = formatBytes(bytes * 50);
  const hundredPages = formatBytes(bytes * 100);
  const twoHundredPages = formatBytes(bytes * 200);

  return (
    <div>
      <div>
        {translate('50PagesFifty', [fiftyPages])}
      </div>
      <div>
        {translate('100PagesHundred', [hundredPages])}
      </div>
      <div>
        {translate('200PagesTwoHundred', [twoHundredPages])}
      </div>
    </div>
  );
}

QualityDefinitionLimits.propTypes = {
  bytes: PropTypes.number,
  message: PropTypes.string.isRequired
};

export default QualityDefinitionLimits;
