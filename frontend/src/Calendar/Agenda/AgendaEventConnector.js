import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createQueueItemSelector from 'Store/Selectors/createQueueItemSelector';
import createSeriesSelector from 'Store/Selectors/createSeriesSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import AgendaEvent from './AgendaEvent';

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
        longDateFormat: uiSettings.longDateFormat,
        colorImpairedMode: uiSettings.enableColorImpairedMode
      };
    }
  );
}

export default connect(createMapStateToProps)(AgendaEvent);
