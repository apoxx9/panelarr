import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import translate from 'Utilities/String/translate';
import styles from './IssueshelfIssue.css';

class IssueshelfIssue extends Component {

  //
  // Listeners

  onIssueMonitoredPress = () => {
    const {
      id,
      monitored
    } = this.props;

    this.props.onIssueMonitoredPress(id, !monitored);
  };

  //
  // Render

  render() {
    const {
      title,
      disambiguation,
      monitored,
      statistics = {},
      isSaving
    } = this.props;

    const {
      issueCount = 0,
      issueFileCount = 0,
      totalIssueCount = 0,
      percentOfIssues = 0
    } = statistics;

    return (
      <div className={styles.issue}>
        <div className={styles.info}>
          <MonitorToggleButton
            monitored={monitored}
            isSaving={isSaving}
            onPress={this.onIssueMonitoredPress}
          />

          <span>
            {
              disambiguation ? `${title} (${disambiguation})` : `${title}`
            }
          </span>
        </div>

        <div
          className={classNames(
            styles.issues,
            percentOfIssues < 100 && monitored && styles.missingWanted,
            percentOfIssues === 100 && styles.allIssues
          )}
          title={translate('IssueProgressBarText', {
            issueCount: issueFileCount ? issueCount : 0,
            issueFileCount,
            totalIssueCount
          })}
        >
          {
            totalIssueCount === 0 ? '0/0' : `${issueFileCount ? issueCount : 0}/${totalIssueCount}`
          }
        </div>
      </div>
    );
  }
}

IssueshelfIssue.propTypes = {
  id: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  monitored: PropTypes.bool.isRequired,
  statistics: PropTypes.object.isRequired,
  isSaving: PropTypes.bool.isRequired,
  onIssueMonitoredPress: PropTypes.func.isRequired
};

IssueshelfIssue.defaultProps = {
  isSaving: false,
  statistics: {
    issueFileCount: 0,
    totalIssueCount: 0,
    percentOfIssues: 0
  }
};

export default IssueshelfIssue;
