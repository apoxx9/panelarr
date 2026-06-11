import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchHistory, markAsFailed } from 'Store/Actions/historyActions';
import createIssueSelector from 'Store/Selectors/createIssueSelector';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import SeriesHistoryRow from './SeriesHistoryRow';

function createMapStateToProps() {
  return createSelector(
    createSeriesSelector(),
    createIssueSelector(),
    (series, issue) => {
      return {
        series,
        issue
      };
    }
  );
}

const mapDispatchToProps = {
  fetchHistory,
  markAsFailed
};

export default connect(createMapStateToProps, mapDispatchToProps)(SeriesHistoryRow);
