import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { toggleIssuesMonitored } from 'Store/Actions/issueActions';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import IssueDetailsHeader from './IssueDetailsHeader';

function createMapStateToProps() {
  return createSelector(
    createIssueSelector(),
    createUISettingsSelector(),
    createDimensionsSelector(),
    (issue, uiSettings, dimensions) => {

      return {
        ...issue,
        overview: issue?.overview || '',
        shortDateFormat: uiSettings.shortDateFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

const mapDispatchToProps = {
  toggleIssuesMonitored
};

class IssueDetailsHeaderConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {
    this.props.toggleIssuesMonitored({
      issueIds: [this.props.issueId],
      monitored
    });
  };

  //
  // Render

  render() {
    return (
      <IssueDetailsHeader
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

IssueDetailsHeaderConnector.propTypes = {
  issueId: PropTypes.number,
  toggleIssuesMonitored: PropTypes.func.isRequired,
  series: PropTypes.object
};

export default connect(createMapStateToProps, mapDispatchToProps)(IssueDetailsHeaderConnector);
