import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import { isCommandExecuting } from 'Utilities/Command';
import IssueSearchCell from './IssueSearchCell';

function createMapStateToProps() {
  return createSelector(
    (state, { issueId }) => issueId,
    createSeriesSelector(),
    createCommandsSelector(),
    (issueId, series, commands) => {
      const isSearching = commands.some((command) => {
        const issueSearch = command.name === commandNames.ISSUE_SEARCH;

        if (!issueSearch) {
          return false;
        }

        return (
          isCommandExecuting(command) &&
          command.body.issueIds.indexOf(issueId) > -1
        );
      });

      return {
        seriesMonitored: series.monitored,
        isSearching
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSearchPress(name, path) {
      dispatch(executeCommand({
        name: commandNames.ISSUE_SEARCH,
        issueIds: [props.issueId]
      }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IssueSearchCell);
