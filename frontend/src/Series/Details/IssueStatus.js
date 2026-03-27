import PropTypes from 'prop-types';
import React from 'react';
import IssueQuality from 'Issue/IssueQuality';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './IssueStatus.css';

function IssueStatus(props) {
  const {
    isAvailable,
    monitored,
    issueFile
  } = props;

  const hasIssueFile = !!issueFile;

  if (hasIssueFile) {
    const quality = issueFile.quality;

    return (
      <div className={styles.center}>
        <IssueQuality
          title={quality.quality.name}
          size={issueFile.size}
          quality={quality}
          isMonitored={monitored}
          isCutoffNotMet={issueFile.qualityCutoffNotMet}
        />
      </div>
    );
  }

  if (!monitored) {
    return (
      <div className={styles.center}>
        <Label
          title={translate('NotMonitored')}
          kind={kinds.WARNING}
        >
          {translate('NotMonitored')}
        </Label>
      </div>
    );
  }

  if (isAvailable) {
    return (
      <div className={styles.center}>
        <Label
          title={translate('IssueAvailableButMissing')}
          kind={kinds.DANGER}
        >
          {translate('Missing')}
        </Label>
      </div>
    );
  }

  return (
    <div className={styles.center}>
      <Label
        title={translate('NotAvailable')}
        kind={kinds.INFO}
      >
        {translate('NotAvailable')}
      </Label>
    </div>
  );
}

IssueStatus.propTypes = {
  isAvailable: PropTypes.bool,
  monitored: PropTypes.bool.isRequired,
  issueFile: PropTypes.object
};

export default IssueStatus;
