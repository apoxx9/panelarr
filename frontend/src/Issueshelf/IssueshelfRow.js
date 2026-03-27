import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SeriesNameLink from 'Series/SeriesNameLink';
import { getSeriesStatusDetails } from 'Series/SeriesStatus';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import IssueshelfIssue from './IssueshelfIssue';
import styles from './IssueshelfRow.css';

class IssueshelfRow extends Component {

  //
  // Render

  render() {
    const {
      seriesId,
      status,
      titleSlug,
      seriesName,
      monitored,
      issues,
      isSaving,
      isSelected,
      onSelectedChange,
      onSeriesMonitoredPress,
      onIssueMonitoredPress
    } = this.props;

    const statusDetails = getSeriesStatusDetails(status);

    return (
      <>
        <VirtualTableSelectCell
          className={styles.selectCell}
          id={seriesId}
          isSelected={isSelected}
          onSelectedChange={onSelectedChange}
          isDisabled={false}
        />

        <VirtualTableRowCell className={styles.monitored}>
          <MonitorToggleButton
            monitored={monitored}
            size={14}
            isSaving={isSaving}
            onPress={onSeriesMonitoredPress}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.status}>
          <Icon
            className={styles.statusIcon}
            name={statusDetails.icon}
            title={statusDetails.title}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.title}>
          <SeriesNameLink
            titleSlug={titleSlug}
            seriesName={seriesName}
          />
        </VirtualTableRowCell>

        <VirtualTableRowCell className={styles.issues}>
          {
            issues.map((issue) => {
              return (
                <IssueshelfIssue
                  key={issue.id}
                  {...issue}
                  onIssueMonitoredPress={onIssueMonitoredPress}
                />
              );
            })
          }
        </VirtualTableRowCell>
      </>
    );
  }
}

IssueshelfRow.propTypes = {
  seriesId: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  titleSlug: PropTypes.string.isRequired,
  seriesName: PropTypes.string.isRequired,
  monitored: PropTypes.bool.isRequired,
  issues: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSaving: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onSeriesMonitoredPress: PropTypes.func.isRequired,
  onIssueMonitoredPress: PropTypes.func.isRequired
};

IssueshelfRow.defaultProps = {
  isSaving: false
};

export default IssueshelfRow;
