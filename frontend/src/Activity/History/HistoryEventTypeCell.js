import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import styles from './HistoryEventTypeCell.css';

function getIconName(eventType) {
  switch (eventType) {
    case 'grabbed':
      return icons.DOWNLOADING;
    case 'seriesFolderImported':
      return icons.DRIVE;
    case 'issueFileImported':
      return icons.DOWNLOADED;
    case 'downloadFailed':
      return icons.DOWNLOADING;
    case 'issueFileDeleted':
      return icons.DELETE;
    case 'issueFileRenamed':
      return icons.ORGANIZE;
    case 'issueFileRetagged':
      return icons.RETAG;
    case 'issueImportIncomplete':
      return icons.DOWNLOADED;
    case 'downloadIgnored':
      return icons.IGNORE;
    default:
      return icons.UNKNOWN;
  }
}

function getIconKind(eventType) {
  switch (eventType) {
    case 'downloadFailed':
      return kinds.DANGER;
    case 'issueImportIncomplete':
      return kinds.WARNING;
    default:
      return kinds.DEFAULT;
  }
}

function getTooltip(eventType, data) {
  switch (eventType) {
    case 'grabbed':
      return `Issue grabbed from ${data.indexer} and sent to ${data.downloadClient}`;
    case 'seriesFolderImported':
      return 'Issue imported from series folder';
    case 'issueFileImported':
      return 'Issue downloaded successfully and picked up from download client';
    case 'downloadFailed':
      return 'Issue download failed';
    case 'issueFileDeleted':
      return 'Issue file deleted';
    case 'issueFileRenamed':
      return 'Issue file renamed';
    case 'issueFileRetagged':
      return 'Issue file tags updated';
    case 'issueImportIncomplete':
      return 'Files downloaded but not all could be imported';
    case 'downloadIgnored':
      return 'Issue Download Ignored';
    default:
      return 'Unknown event';
  }
}

function HistoryEventTypeCell({ eventType, data }) {
  const iconName = getIconName(eventType);
  const iconKind = getIconKind(eventType);
  const tooltip = getTooltip(eventType, data);

  return (
    <TableRowCell
      className={styles.cell}
      title={tooltip}
    >
      <Icon
        name={iconName}
        kind={iconKind}
      />
    </TableRowCell>
  );
}

HistoryEventTypeCell.propTypes = {
  eventType: PropTypes.string.isRequired,
  data: PropTypes.object
};

HistoryEventTypeCell.defaultProps = {
  data: {}
};

export default HistoryEventTypeCell;
