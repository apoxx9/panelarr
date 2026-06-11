import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createQueueItemSelector from 'Store/Selectors/createQueueItemSelector';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import CalendarEvent from './CalendarEvent';

function createMapStateToProps() {
  return createSelector(
    createSeriesSelector(),
    createQueueItemSelector(),
    createUISettingsSelector(),
    (series, queueItem, uiSettings) => {
      return {
        series,
        queueItem,
        timeFormat: uiSettings.timeFormat,
        colorImpairedMode: uiSettings.enableColorImpairedMode
      };
    }
  );
}

export default connect(createMapStateToProps)(CalendarEvent);
