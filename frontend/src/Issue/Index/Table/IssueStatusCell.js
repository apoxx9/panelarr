import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './IssueStatusCell.css';

function IssueStatusCell(props) {
  const {
    className,
    monitored,
    component: Component,
    ...otherProps
  } = props;

  return (
    <Component
      className={className}
      {...otherProps}
    >
      <Icon
        className={styles.statusIcon}
        name={monitored ? icons.MONITORED : icons.UNMONITORED}
        title={monitored ? translate('MonitoredSeriesIsMonitored') : translate('MonitoredSeriesIsUnmonitored')}
      />
    </Component>
  );
}

IssueStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  component: PropTypes.elementType
};

IssueStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default IssueStatusCell;
